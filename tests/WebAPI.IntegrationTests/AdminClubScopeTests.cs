using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md §25 "Çıkış koşulu" (K-31/A-55/Y-66) — Faz 25'in kabul testi.
///
/// v4.0'a kadar yönetici, danışmanı olmadığı bir kulüpte 4 uçta birden kilitliydi: etkinlik
/// listesi/oluşturma, duyuru yazma, üye rolü değiştirme ve logo yükleme. Arayüz düğmeleri
/// izinlere göre gösteriyordu, API kapsam kuralı yüzünden 403 dönüyordu — "izin var ama kapsam yok".
///
/// Bu test hem yeni yolun açıldığını hem de <b>izni olmayanın hâlâ giremediğini</b> kanıtlar.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AdminClubScopeTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "acs-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    /// <summary>Kulüple hiçbir bağı olmayan, ama events/announcements/files izinleri taşıyan kullanıcı.</summary>
    private const string OutsiderEmail = "acs-outsider@test.local";
    private const string OutsiderPassword = "Outsider!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _clubId;
    private int _membershipId;

    public AdminClubScopeTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, IdentitySeedData.AdminRoleName);

        // ClubOfficer: events.write + announcements.write + files.upload + memberships.write taşır,
        // ama clubs.manage.all TAŞIMAZ. Kapsam kuralının hâlâ ısırdığı yer burası.
        await EnsureUserAsync(userManager, OutsiderEmail, OutsiderPassword, IdentitySeedData.ClubOfficerRoleName);

        var department = await db.Departments.FirstAsync();
        var term = await db.AcademicTerms.FirstAsync(t => t.IsCurrent);

        // Danışmanı BAŞKA biri olan bir kulüp — ne admin ne de outsider bu kulübün danışmanı.
        var advisorUser = await EnsureUserAsync(userManager, "acs-advisor@test.local", "Advisor!Test123456", IdentitySeedData.AdvisorRoleName);
        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == advisorUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Doç. Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var club = new Club
        {
            Name = $"Kapsam Kulubu {suffix}",
            AdvisorId = advisor.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        _clubId = club.Id;

        // Rol değiştirme ucunun hedefi olacak bir üye.
        var memberUser = await EnsureUserAsync(userManager, $"acs-member-{suffix}@test.local", "Member!Test123456", IdentitySeedData.MemberRoleName);
        var student = new Student
        {
            ApplicationUserId = memberUser.Id,
            StudentNumber = $"AC{suffix}"[..10],
            DepartmentId = department.Id,
            EnrollmentYear = 2026,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var membership = new ClubMembership
        {
            ClubId = _clubId,
            StudentId = student.Id,
            AcademicTermId = term.Id,
            ClubRole = ClubRole.Member,
            JoinedAtUtc = DateTime.UtcNow,
        };
        db.ClubMemberships.Add(membership);
        await db.SaveChangesAsync();
        _membershipId = membership.Id;

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-55: yönetici, danışmanı olmadığı kulübün ETKİNLİKLERİNİ görebiliyor ve oluşturabiliyor")]
    public async Task Admin_CanReadAndCreateClubEvents()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        var list = await SendAsync(HttpMethod.Get, $"/api/clubs/{_clubId}/events?pageIndex=0&pageSize=20", token);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/events", token, NewEvent("Yonetici Etkinligi"));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
    }

    [Fact(DisplayName = "A-55: yönetici kulübe DUYURU yazabiliyor")]
    public async Task Admin_CanWriteClubAnnouncement()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/clubs/{_clubId}/announcements",
            token,
            new { Title = "Yonetici Duyurusu", Content = "Kapsam acildi.", Visibility = "Members" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "A-55: yönetici ÜYE ROLÜ değiştirebiliyor")]
    public async Task Admin_CanChangeMemberRole()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(
            HttpMethod.Put,
            $"/api/clubs/{_clubId}/members/{_membershipId}/role",
            token,
            new { ClubRole = "Officer" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "A-55: yönetici kulüp LOGOSU yükleyebiliyor (önceden düğme gösterilip garanti 403 alınıyordu)")]
    public async Task Admin_CanUploadClubLogo()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        using var content = new MultipartFormDataContent();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "logo.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/clubs/{_clubId}/logo") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Y-66'nın ret tarafı: clubs.manage.all TAŞIMAYAN kullanıcı dört ucun hiçbirine giremiyor")]
    public async Task WithoutPermission_AllFourEndpointsForbidden()
    {
        var token = await LoginAsync(OutsiderEmail, OutsiderPassword);

        var events = await SendAsync(HttpMethod.Get, $"/api/clubs/{_clubId}/events?pageIndex=0&pageSize=20", token);
        var createEvent = await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/events", token, NewEvent("Olmamali"));
        var announcement = await SendAsync(
            HttpMethod.Post,
            $"/api/clubs/{_clubId}/announcements",
            token,
            new { Title = "Olmamali", Content = "Olmamali", Visibility = "Members" });
        var role = await SendAsync(
            HttpMethod.Put,
            $"/api/clubs/{_clubId}/members/{_membershipId}/role",
            token,
            new { ClubRole = "President" });

        Assert.Equal(HttpStatusCode.Forbidden, events.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createEvent.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, announcement.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, role.StatusCode);
    }

    private static object NewEvent(string title) => new
    {
        Title = title,
        Description = "Kapsam testi",
        Location = "A Salonu",
        StartDateUtc = DateTime.UtcNow.AddDays(7),
        EndDateUtc = DateTime.UtcNow.AddDays(7).AddHours(2),
        Capacity = 25,
    };

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

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
