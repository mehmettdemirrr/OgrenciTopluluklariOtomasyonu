using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md §26 "Çıkış koşulu" (K-32/A-56/A-57/Y-67) — Faz 26'nın kabul testi.
///
/// v5.0'a kadar <c>POST /api/users</c> yalnızca Identity kaydı yazıyordu. Sonuç: <c>Member</c> rolü
/// verilen kullanıcının <c>Student</c> kaydı olmadığı için kulübe başvuramıyor, etkinliğe
/// kaydolamıyordu — ne kullanıcı ne de onu oluşturan yönetici sebebi görebiliyordu (bulgu 10).
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class UserProvisioningTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "prov-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _departmentId;
    private int _clubId;
    private string _suffix = string.Empty;

    public UserProvisioningTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, IdentitySeedData.AdminRoleName);

        _suffix = Guid.NewGuid().ToString("N")[..8];
        _departmentId = (await db.Departments.FirstAsync()).Id;

        var advisorUser = await EnsureUserAsync(userManager, $"prov-advisor-{_suffix}@test.local", "Advisor!Test123456", IdentitySeedData.AdvisorRoleName);
        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = _departmentId };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Provizyon Kulubu {_suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        _clubId = club.Id;

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Y-67: Member rolüyle oluşturulan kullanıcının Student kaydı var ve GERÇEKTEN kulübe başvurabiliyor")]
    public async Task CreateUser_WithMemberRole_CreatesUsableStudentProfile()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var email = $"prov-student-{_suffix}@test.local";
        const string password = "Student!Test123456";

        var create = await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = email,
            Password = password,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            RoleNames = new[] { IdentitySeedData.MemberRoleName },
            StudentNumber = $"PR{_suffix}"[..10],
            DepartmentId = _departmentId,
            EnrollmentYear = 2026,
        });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == email);

            Assert.Equal("Ayse", user.FirstName);
            Assert.Equal("Yilmaz", user.LastName);
            Assert.True(await db.Students.AnyAsync(s => s.ApplicationUserId == user.Id), "Student profili oluşturulmadı.");
        }

        // Asıl kanıt: kullanıcı yalnızca "var olmakla" kalmıyor, sistemi KULLANABİLİYOR.
        var studentToken = await LoginAsync(email, password);
        var apply = await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/membership-applications", studentToken);

        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);
    }

    [Fact(DisplayName = "Y-67: Advisor rolüyle oluşturulan kullanıcının AcademicStaff kaydı var (kulübe danışman atanabilir)")]
    public async Task CreateUser_WithAdvisorRole_CreatesAcademicStaffProfile()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var email = $"prov-newadvisor-{_suffix}@test.local";

        var create = await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = email,
            Password = "Advisor!Test123456",
            FirstName = "Mehmet",
            LastName = "Demir",
            RoleNames = new[] { IdentitySeedData.AdvisorRoleName },
            DepartmentId = _departmentId,
            Title = "Prof. Dr.",
        });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);

        Assert.True(await db.AcademicStaff.AnyAsync(a => a.ApplicationUserId == user.Id), "AcademicStaff profili oluşturulmadı.");
    }

    [Fact(DisplayName = "Y-67: profil alanları eksikse kullanıcı HİÇ oluşturulmuyor (yarım kullanıcı yasak)")]
    public async Task CreateUser_MissingProfileFields_CreatesNothing()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var email = $"prov-incomplete-{_suffix}@test.local";

        var create = await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = email,
            Password = "Student!Test123456",
            RoleNames = new[] { IdentitySeedData.MemberRoleName },
            // StudentNumber, DepartmentId, EnrollmentYear kasıtlı olarak yok.
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Kritik: Identity kaydı da oluşmamalı — aksi hâlde geriye yarım kullanıcı kalırdı.
        Assert.False(await db.Users.AnyAsync(u => u.Email == email), "Doğrulama başarısız olmasına rağmen kullanıcı oluşturuldu.");
    }

    [Fact(DisplayName = "A-56: kullanıcı listesi ad soyad ve durum taşıyor, ARAMA ada göre de çalışıyor")]
    public async Task UserList_CarriesNameAndStatus_AndSearchesByName()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var email = $"prov-search-{_suffix}@test.local";

        await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = email,
            Password = "Search!Test123456",
            FirstName = "Zeynep",
            LastName = $"Kara{_suffix}",
            RoleNames = Array.Empty<string>(),
        });

        var response = await SendAsync(HttpMethod.Get, $"/api/users?pageIndex=0&pageSize=20&search=Kara{_suffix}", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = (await response.Content.ReadFromJsonAsync<PagedUsers>())!;
        var row = Assert.Single(paged.Items);

        Assert.Equal("Zeynep", row.FirstName);
        Assert.Equal($"Kara{_suffix}", row.LastName);
        Assert.False(row.IsLockedOut);
    }

    [Fact(DisplayName = "A-57: bağlı kaydı olan kullanıcı silinemiyor (409), bağsız kullanıcı silinebiliyor")]
    public async Task DeleteUser_BlockedByDomainRecords_ButAllowedWhenFree()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        // 1) Bağsız kullanıcı: silinebilmeli.
        var freeEmail = $"prov-free-{_suffix}@test.local";
        var freeCreate = await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = freeEmail,
            Password = "Free!Test123456",
            RoleNames = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.OK, freeCreate.StatusCode);

        int freeUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            freeUserId = (await db.Users.SingleAsync(u => u.Email == freeEmail)).Id;
        }

        var freeDelete = await SendAsync(HttpMethod.Delete, $"/api/users/{freeUserId}", adminToken);
        Assert.Equal(HttpStatusCode.OK, freeDelete.StatusCode);

        // 2) Danışmanlık yapan kullanıcı: silinememeli.
        int advisorUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            advisorUserId = (await db.Users.SingleAsync(u => u.Email == $"prov-advisor-{_suffix}@test.local")).Id;
        }

        var advisorDelete = await SendAsync(HttpMethod.Delete, $"/api/users/{advisorUserId}", adminToken);
        Assert.Equal(HttpStatusCode.Conflict, advisorDelete.StatusCode);
    }

    [Fact(DisplayName = "Y-03/A-57: yönetici kendi hesabını silemiyor")]
    public async Task DeleteUser_Self_ReturnsConflict()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        int adminId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            adminId = (await db.Users.SingleAsync(u => u.Email == AdminEmail)).Id;
        }

        var response = await SendAsync(HttpMethod.Delete, $"/api/users/{adminId}", adminToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Y-58: ad soyad anonim vitrin uçlarından dönmüyor")]
    public async Task PublicEndpoints_DoNotLeakNames()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var lastName = $"Sizinti{_suffix}";

        await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = $"prov-leak-{_suffix}@test.local",
            Password = "Leak!Test123456",
            FirstName = "Gizli",
            LastName = lastName,
            RoleNames = Array.Empty<string>(),
        });

        foreach (var path in new[] { "/api/public/clubs", "/api/public/events", "/api/public/announcements" })
        {
            var body = await _client.GetStringAsync($"{path}?pageIndex=0&pageSize=100");

            Assert.DoesNotContain(lastName, body, StringComparison.Ordinal);
            Assert.DoesNotContain("firstName", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lastName", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string requestUri, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    private sealed class PagedUsers
    {
        public List<UserRow> Items { get; init; } = [];
    }

    private sealed class UserRow
    {
        public int Id { get; init; }

        public string Email { get; init; } = string.Empty;

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public bool IsLockedOut { get; init; }
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
