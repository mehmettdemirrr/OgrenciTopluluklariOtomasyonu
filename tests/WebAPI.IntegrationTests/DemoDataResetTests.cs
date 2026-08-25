using DataAccess;
using Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md · Faz 28 (Y-68): <c>Seed:ResetDemo=true</c> demo veriyi silip yeniden üretir.
///
/// Bu testin taut olmayan kısmı şu: sıfırlamanın <b>gerçekten sildiğini</b> kanıtlamak. Aynı
/// sayıyı görmek yetmez — seeder hiç çalışmasa da sayı aynı kalırdı. Bu yüzden kulüp Id'lerinin
/// tümüyle değişmiş olması aranır: eski satırlar fiilen gitmiş, yenileri yeni kimlik almıştır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class DemoDataResetTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private WebApplicationFactory<Program> _seedFactory = null!;

    public DemoDataResetTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _seedFactory = CreateFactory(reset: false);

        using var scope = _seedFactory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _seedFactory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "Seed:ResetDemo demo veriyi siler ve yeniden üretir")]
    public async Task ResetDemo_PurgesAndRebuilds()
    {
        List<int> clubIdsBefore;
        List<string?> emailsBefore;
        int recordCountBefore;

        using (var scope = _seedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clubIdsBefore = await TaggedClubIdsAsync(db);
            emailsBefore = await DemoEmailsAsync(db);
            recordCountBefore = await db.DemoSeedRecords.CountAsync();
        }

        Assert.Equal(8, clubIdsBefore.Count);

        using var resetFactory = CreateFactory(reset: true);
        using var resetScope = resetFactory.Services.CreateScope();
        var resetDb = resetScope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Aynı miktarda veri, aynı hesaplar — ama tamamen yeni satırlar. Id'lerin kesişmemesi,
        // "hiç çalışmasa da aynı sayıyı görürdüm" itirazını kapatan kısımdır.
        Assert.Equal(clubIdsBefore.Count, (await TaggedClubIdsAsync(resetDb)).Count);
        Assert.Equal(recordCountBefore, await resetDb.DemoSeedRecords.CountAsync());
        Assert.Equal(emailsBefore, await DemoEmailsAsync(resetDb));
        Assert.Empty(clubIdsBefore.Intersect(await TaggedClubIdsAsync(resetDb)));
    }

    [Fact(DisplayName = "Sıfırlama künyesiz kayıtlara dokunmaz (Y-68)")]
    public async Task ResetDemo_LeavesUntaggedRecordsAlone()
    {
        int untaggedClubId;
        int untaggedUserId;

        using (var scope = _seedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Gerçek verinin temsilcisi: künyesiz kullanıcı + künyesiz danışman + künyesiz kulüp.
            // Demo zincirinden tamamen bağımsız kurulur — aksi hâlde test "sıfırlama gerçek veriyi
            // korudu mu"yu değil, "FK kısıtı sildirmedi mi"yi ölçerdi.
            var user = new ApplicationUser
            {
                UserName = "gercek-danisman@test.local",
                Email = "gercek-danisman@test.local",
                EmailConfirmed = true,
                FirstName = "Gerçek",
                LastName = "Danışman",
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            untaggedUserId = user.Id;

            var staff = new AcademicStaff { ApplicationUserId = user.Id, Title = "Prof. Dr.", DepartmentId = 1 };
            db.AcademicStaff.Add(staff);
            await db.SaveChangesAsync();

            var club = new Club
            {
                Name = "Gerçek Veri Topluluğu",
                Description = "Demo seeder tarafından üretilmedi.",
                AdvisorId = staff.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Clubs.Add(club);
            await db.SaveChangesAsync();
            untaggedClubId = club.Id;
        }

        using var resetFactory = CreateFactory(reset: true);
        using var resetScope = resetFactory.Services.CreateScope();
        var resetDb = resetScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await resetDb.Clubs.AnyAsync(c => c.Id == untaggedClubId));
        Assert.True(await resetDb.Users.AnyAsync(u => u.Id == untaggedUserId));
        Assert.True(await resetDb.AcademicStaff.AnyAsync(s => s.ApplicationUserId == untaggedUserId));
    }

    private static async Task<List<int>> TaggedClubIdsAsync(AppDbContext db)
    {
        var ids = await db.DemoSeedRecords
            .Where(r => r.EntityType == DemoEntityTypes.Club)
            .Select(r => r.EntityId)
            .ToListAsync();

        return [.. ids.OrderBy(id => id)];
    }

    private static Task<List<string?>> DemoEmailsAsync(AppDbContext db) =>
        db.Users
            .Where(u => u.Email!.EndsWith(".invalid"))
            .Select(u => u.Email)
            .OrderBy(e => e)
            .ToListAsync();

    private WebApplicationFactory<Program> CreateFactory(bool reset) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seed:Demo"] = "true",
                    ["Seed:DemoPassword"] = "Demo!Test123456",
                    ["Seed:ResetDemo"] = reset ? "true" : "false",
                })));
}
