using System.Net;
using System.Net.Http.Json;
using DataAccess;
using DataAccess.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md · Faz 28 (K-34, A-58): kurumsal referans verisi migration ile gelir.
///
/// Bu testlerin asıl konusu <b>sayı değil, kimlik korunumu</b>: Faz 28 öncesi seed'in ürettiği
/// <c>Faculty Id=1</c> / <c>Department Id=1</c> satırlarına demo öğrencinin yabancı anahtarı
/// bağlı. Migration onları silip yeniden ekleseydi FK kısıtına takılır ve mevcut veritabanları
/// güncellenemezdi — bu yüzden "Id 1 hâlâ orada mı" sorusu regresyon testidir.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class InstitutionalReferenceDataTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public InstitutionalReferenceDataTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Migration'daki SQL, DomainSeedData katalogunun tamamını yazar")]
    public async Task Migration_WritesTheWholeCatalog()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var faculties = await db.Faculties.ToDictionaryAsync(f => f.Name, f => f.Id);
        Assert.Equal(19, faculties.Count);
        foreach (var expected in DomainSeedData.FacultyNames())
        {
            Assert.Contains(expected, faculties.Keys);
        }

        var departments = await db.Departments
            .Select(d => new { d.Name, d.FacultyId })
            .ToListAsync();

        Assert.Equal(121, departments.Count);

        // SQL migration dosyasına gömülü, katalog ise ayrı bir dosyada. Bu döngü ikisinin
        // ayrışmadığını sınar — Faz 28'in tek gerçek regresyon riski budur.
        foreach (var entry in DomainSeedData.Departments())
        {
            Assert.True(
                departments.Any(d => d.Name == entry.Name && d.FacultyId == faculties[entry.FacultyName]),
                $"Katalogdaki bölüm veritabanında yok: {entry.FacultyName} / {entry.Name}");
        }
    }

    [Fact(DisplayName = "Faculty Id=1 silinmez, yalnızca yeniden adlandırılır ve Department Id=1 altında kalır")]
    public async Task Migration_KeepsPinnedIdentityRows()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var faculty = await db.Faculties.SingleAsync(f => f.Id == DomainSeedData.FacultyId);
        Assert.Equal(DomainSeedData.PinnedFacultyName, faculty.Name);

        var department = await db.Departments.SingleAsync(d => d.Id == DomainSeedData.DepartmentId);
        Assert.Equal(DomainSeedData.PinnedDepartmentName, department.Name);
        Assert.Equal(DomainSeedData.FacultyId, department.FacultyId);
    }

    [Fact(DisplayName = "Referans veri SQL'i idempotenttir — ikinci kez çalıştırılınca satır eklemez")]
    public async Task ReferenceDataSql_IsIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facultiesBefore = await db.Faculties.CountAsync();
        var departmentsBefore = await db.Departments.CountAsync();

        // Migration'ın SQL'i, uygulandığı veritabanının durumundan bağımsız olmalı: aynı betiği
        // dolu bir veritabanına yeniden uygulamak hiçbir şeyi değiştirmemeli. HasData'nın
        // yapamadığı — ve bu yüzden terk edildiği — şey tam olarak budur.
        await db.Database.ExecuteSqlRawAsync(ReferenceDataSql());

        Assert.Equal(facultiesBefore, await db.Faculties.CountAsync());
        Assert.Equal(departmentsBefore, await db.Departments.CountAsync());
    }

    /// <summary>Katalogdan, migration'daki ile aynı biçimde idempotent SQL üretir.</summary>
    private static string ReferenceDataSql()
    {
        var statements = new List<string>();

        foreach (var name in DomainSeedData.FacultyNames())
        {
            statements.Add(
                $"INSERT INTO Faculties (Name) SELECT {Quote(name)} " +
                $"WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = {Quote(name)});");
        }

        foreach (var entry in DomainSeedData.Departments())
        {
            statements.Add(
                $"INSERT INTO Departments (Name, FacultyId) SELECT {Quote(entry.Name)}, f.Id FROM Faculties f " +
                $"WHERE f.Name = {Quote(entry.FacultyName)} AND NOT EXISTS " +
                $"(SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = {Quote(entry.Name)});");
        }

        return string.Join('\n', statements);
    }

    private static string Quote(string value) => "N'" + value.Replace("'", "''") + "'";

    [Fact(DisplayName = "Aynı bölüm adı farklı fakültelerde tekrar edebilir")]
    public async Task Departments_AllowSameNameUnderDifferentFaculties()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Akçadağ ve Arapgir MYO'nun ikisinde de "Bilgisayar Teknolojileri" var — (FacultyId, Name)
        // benzersizliğinin Name tekilliği OLMADIĞININ kanıtı.
        var duplicates = await db.Departments
            .Where(d => d.Name == "Bilgisayar Teknolojileri")
            .Select(d => d.FacultyId)
            .ToListAsync();

        Assert.True(duplicates.Count >= 2);
        Assert.Equal(duplicates.Count, duplicates.Distinct().Count());
    }

    [Fact(DisplayName = "Kayıt formu bölüm listesi 121 satır döner ve fakülte adına göre sıralıdır")]
    public async Task RegistrationDepartments_ReturnsAllDepartmentsGroupedByFaculty()
    {
        var response = await _client.GetAsync("/api/auth/departments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var departments = await response.Content.ReadFromJsonAsync<List<RegistrationDepartmentRow>>();
        Assert.NotNull(departments);
        Assert.Equal(121, departments!.Count);
        Assert.All(departments, d => Assert.False(string.IsNullOrWhiteSpace(d.FacultyName)));

        // Arayüz gruplu bir Autocomplete çiziyor; MUI grupları yalnızca BİTİŞİK öğeler için
        // birleştirir. Her fakülte adının listede tek bir kesintisiz blok oluşturması şart.
        var facultyBlocks = departments
            .Select(d => d.FacultyName)
            .Aggregate(new List<string>(), (blocks, name) =>
            {
                if (blocks.Count == 0 || blocks[^1] != name)
                {
                    blocks.Add(name);
                }

                return blocks;
            });

        Assert.Equal(19, facultyBlocks.Count);
        Assert.Equal(facultyBlocks.Count, facultyBlocks.Distinct().Count());
    }

    [Fact(DisplayName = "A-58/A-62: sekiz gerçek MTÜ formu HasData ile seed edilir ve hepsi zorunludur")]
    public async Task ClubDocumentTypes_EightRealFormsAreSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seeded = await db.ClubDocumentTypes.AsNoTracking().OrderBy(t => t.DisplayOrder).ToListAsync();

        string[] expectedCodes =
            ["FR-0230", "FR-0240", "FR-0241", "FR-0242", "FR-0243", "FR-0244", "FR-0245", "FR-0272"];

        Assert.Equal(expectedCodes, seeded.Select(t => t.Code).ToArray());
        Assert.All(seeded, t => Assert.True(t.IsRequired));
        Assert.All(seeded, t => Assert.True(t.IsActive));
        Assert.All(seeded, t => Assert.False(string.IsNullOrWhiteSpace(t.Name)));
    }

    private sealed record RegistrationDepartmentRow(int Id, string Name, string FacultyName);
}
