using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataAccess;
using Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>docs/MIMARI.md · K-44/A-74/Y-80: kulüp iletişimi ve sosyal bağlantılar.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class ClubSocialLinkTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ClubSocialLinkTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Yönetici olmayan sıradan üye iletişim bilgisini değiştiremez (Forbidden)")]
    public async Task SetContact_Rejects_ForPlainMember()
    {
        var scenario = await SeedScenarioAsync("csl-plain");
        var token = await LoginAndGetAccessTokenAsync(scenario.PlainMemberEmail, scenario.PlainMemberPassword);

        var response = await SendWithBearerAsync(HttpMethod.Put, $"/api/clubs/{scenario.ClubId}/contact", token, ValidPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "AnnouncementsManage kapasiteli yetkili iletişim bilgisini değiştirir ve eski bağlantılar değişir")]
    public async Task SetContact_ReplacesLinks_ForOfficer()
    {
        var scenario = await SeedScenarioAsync("csl-officer");
        var token = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);

        var firstResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/clubs/{scenario.ClubId}/contact", token, ValidPayload());
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/clubs/{scenario.ClubId}/contact", token, SingleLinkPayload());
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var detailResponse = await SendWithBearerAsync(HttpMethod.Get, $"/api/clubs/{scenario.ClubId}", token);
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, detail.GetProperty("socialLinks").GetArrayLength());
        Assert.Equal("iletisim@kulup.test", detail.GetProperty("contactEmail").GetString());
    }

    [Fact(DisplayName = "http:// bağlantısı reddedilir (Y-80)")]
    public async Task SetContact_Rejects_HttpUrl()
    {
        var scenario = await SeedScenarioAsync("csl-http");
        var token = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);

        var payload = new
        {
            ContactEmail = "iletisim@kulup.test",
            ContactPhone = (string?)null,
            Links = new[] { new { Platform = "Instagram", Url = "http://www.instagram.com/kulup", DisplayOrder = 0 } },
        };

        var response = await SendWithBearerAsync(HttpMethod.Put, $"/api/clubs/{scenario.ClubId}/contact", token, payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Anonim ziyaretçi kulübün sosyal bağlantılarını görür")]
    public async Task PublicClubDetail_ExposesSocialLinks_Anonymously()
    {
        var scenario = await SeedScenarioAsync("csl-public");
        var token = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);
        var setResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/clubs/{scenario.ClubId}/contact", token, ValidPayload());
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var anonymousResponse = await _client.GetAsync($"/api/public/clubs/{scenario.ClubId}");

        Assert.Equal(HttpStatusCode.OK, anonymousResponse.StatusCode);
        var detail = await anonymousResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(detail.GetProperty("socialLinks").GetArrayLength() > 0);
        Assert.Equal("iletisim@kulup.test", detail.GetProperty("contactEmail").GetString());
    }

    private static object ValidPayload() => new
    {
        ContactEmail = "iletisim@kulup.test",
        ContactPhone = "+90 555 111 2233",
        Links = new[]
        {
            new { Platform = "Instagram", Url = "https://www.instagram.com/kulup", DisplayOrder = 0 },
            new { Platform = "Website", Url = "https://kulup.ozal.edu.tr", DisplayOrder = 1 },
        },
    };

    private static object SingleLinkPayload() => new
    {
        ContactEmail = "iletisim@kulup.test",
        ContactPhone = (string?)null,
        Links = new[] { new { Platform = "Instagram", Url = "https://www.instagram.com/kulup", DisplayOrder = 0 } },
    };

    private async Task<string> LoginAndGetAccessTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendWithBearerAsync(HttpMethod method, string requestUri, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private sealed record Scenario(int ClubId, string OfficerEmail, string OfficerPassword, string PlainMemberEmail, string PlainMemberPassword);

    private async Task<Scenario> SeedScenarioAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var faculty = new Faculty { Name = $"Fen Fakültesi-{suffix}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var department = new Department { Name = $"Bilgisayar Mühendisliği-{suffix}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        // IX_AcademicTerms_IsCurrent: bu sınıftaki tüm fact'ler AYNI factory/veritabanını paylaşır.
        var term = await db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
        if (term is null)
        {
            term = new AcademicTerm { Name = $"2026-Güz-{suffix}", StartDateUtc = DateTime.UtcNow.AddMonths(-1), EndDateUtc = DateTime.UtcNow.AddMonths(3), IsCurrent = true };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();
        }

        const string advisorPassword = "Advisor!Test123456";
        var advisorEmail = $"csl-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"İletişim Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        const string officerPassword = "Officer!Test123456";
        var officerEmail = $"csl-officer-{suffix}@test.local";
        var officerUser = new ApplicationUser { UserName = officerEmail, Email = officerEmail, EmailConfirmed = true };
        var officerCreateResult = await userManager.CreateAsync(officerUser, officerPassword);
        Assert.True(officerCreateResult.Succeeded, string.Join("; ", officerCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(officerUser, "ClubOfficer");

        var officerStudent = new Student
        {
            ApplicationUserId = officerUser.Id, StudentNumber = $"O{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(officerStudent);
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = officerStudent.Id, AcademicTermId = term.Id,
            ClubRole = ClubRole.Officer,
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.Officer),
            JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        const string plainMemberPassword = "Member!Test123456";
        var plainMemberEmail = $"csl-member-{suffix}@test.local";
        var plainMemberUser = new ApplicationUser { UserName = plainMemberEmail, Email = plainMemberEmail, EmailConfirmed = true };
        var plainMemberCreateResult = await userManager.CreateAsync(plainMemberUser, plainMemberPassword);
        Assert.True(plainMemberCreateResult.Succeeded, string.Join("; ", plainMemberCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(plainMemberUser, "Member");

        var plainMemberStudent = new Student
        {
            ApplicationUserId = plainMemberUser.Id, StudentNumber = $"M{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(plainMemberStudent);
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = plainMemberStudent.Id, AcademicTermId = term.Id,
            ClubRole = ClubRole.Member,
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.Member),
            JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return new Scenario(club.Id, officerEmail, officerPassword, plainMemberEmail, plainMemberPassword);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
