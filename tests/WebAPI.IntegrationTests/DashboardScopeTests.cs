using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V2.md · Faz 12 çıkış koşulu: "Üç farklı rolle giriş → her biri yalnızca kendi
/// kapsamındaki sayıları görüyor (kapsam sızıntısı testi)".
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class DashboardScopeTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public DashboardScopeTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Düz üye: kişisel sayıları görür, yönetim bölümü hiç dönmez")]
    public async Task GetDashboard_PlainMember_ReturnsPersonalOnly()
    {
        var scenario = await SeedAdvisorClubWithMemberAsync("member-scope");
        var token = await LoginAndGetAccessTokenAsync(scenario.StudentEmail, scenario.StudentPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/dashboard", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponseDto>();
        Assert.True(summary!.Personal.MyClubCount >= 1);
        Assert.Null(summary.Management);
    }

    [Fact(DisplayName = "Danışman: yönetim özeti yalnızca KENDİ kulübünü sayar, başka danışmanın kulübü sızmaz")]
    public async Task GetDashboard_Advisor_CountsOnlyOwnClub()
    {
        await SeedAdvisorClubWithMemberAsync("dash-other-advisor");
        var scenario = await SeedAdvisorClubWithMemberAsync("dash-advisor");
        var token = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/dashboard", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponseDto>();
        Assert.NotNull(summary!.Management);
        Assert.False(summary.Management!.AllClubs);
        Assert.Equal(1, summary.Management.ScopeClubCount);
        Assert.Equal(1, summary.Management.ScopeMemberCount);
    }

    [Fact(DisplayName = "reports.read.all izinli admin: AllClubs=true ile tüm kulüpleri kapsayan sayıları görür")]
    public async Task GetDashboard_Admin_SeesAllClubsScope()
    {
        await SeedAdvisorClubWithMemberAsync("dash-admin-scope-1");
        await SeedAdvisorClubWithMemberAsync("dash-admin-scope-2");

        const string adminEmail = "dash-scope-admin@test.local";
        const string adminPassword = "Admin!Test123456";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await EnsureUserAsync(userManager, adminEmail, adminPassword, "Admin");
        }

        var adminToken = await LoginAndGetAccessTokenAsync(adminEmail, adminPassword);
        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/dashboard", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponseDto>();
        Assert.NotNull(summary!.Management);
        Assert.True(summary.Management!.AllClubs);
        Assert.True(summary.Management.ScopeClubCount >= 2, "Admin (reports.read.all) en az iki farklı danışmanın kulübünü görmeli.");
    }

    [Fact(DisplayName = "GET /api/clubs/mine: öğrenci yalnızca kendi üyeliklerini görür, kulüp adı doludur")]
    public async Task GetClubsMine_Student_ReturnsOwnMembershipWithClubName()
    {
        var scenario = await SeedAdvisorClubWithMemberAsync("clubs-mine");
        var token = await LoginAndGetAccessTokenAsync(scenario.StudentEmail, scenario.StudentPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs/mine", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<MyClubMembershipResponseDto>>();
        var item = Assert.Single(items!, i => i.ClubId == scenario.ClubId);
        Assert.False(string.IsNullOrWhiteSpace(item.ClubName));
        Assert.Equal("Member", item.ClubRole);
        // K-41/A-70: üyelik satırı Member ilişkisiyle işaretlenir — danışman satırından ayırt edilsin diye.
        Assert.Equal("Member", item.Relationship);
    }

    [Fact(DisplayName = "K-41/A-70: GET /api/clubs/mine danışmanın kulübünü Advisor ilişkisiyle döner")]
    public async Task GetClubsMine_Advisor_ReturnsAdvisedClubWithAdvisorRelationship()
    {
        var scenario = await SeedAdvisorClubWithMemberAsync("clubs-mine-advisor");
        var token = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs/mine", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<MyClubMembershipResponseDto>>();
        var item = Assert.Single(items!, i => i.ClubId == scenario.ClubId);
        Assert.False(string.IsNullOrWhiteSpace(item.ClubName));
        Assert.Equal("Advisor", item.Relationship);
    }

    private sealed record AdvisorScenario(string AdvisorEmail, string AdvisorPassword, string StudentEmail, string StudentPassword, int ClubId, int TermId);

    private async Task<AdvisorScenario> SeedAdvisorClubWithMemberAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var faculty = new Faculty { Name = $"Fen-{suffix}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var department = new Department { Name = $"Bilgisayar-{suffix}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var term = await db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
        if (term is null)
        {
            term = new AcademicTerm
            {
                Name = $"2026-Güz-{suffix}", StartDateUtc = DateTime.UtcNow.AddMonths(-1), EndDateUtc = DateTime.UtcNow.AddMonths(3), IsCurrent = true,
            };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();
        }

        const string advisorPassword = "Advisor!Test123456";
        var advisorEmail = $"dash-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Panel Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        const string studentPassword = "Student!Test123456";
        var studentEmail = $"dash-student-{suffix}@test.local";
        var studentUser = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
        var studentCreateResult = await userManager.CreateAsync(studentUser, studentPassword);
        Assert.True(studentCreateResult.Succeeded, string.Join("; ", studentCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(studentUser, "Member");

        var student = new Student
        {
            ApplicationUserId = studentUser.Id, StudentNumber = $"S{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = student.Id, AcademicTermId = term.Id, ClubRole = ClubRole.Member, JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return new AdvisorScenario(advisorEmail, advisorPassword, studentEmail, studentPassword, club.Id, term.Id);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendWithBearerAsync(HttpMethod method, string requestUri, string accessToken)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private sealed class DashboardSummaryResponseDto
    {
        public PersonalDashboardStatsResponseDto Personal { get; init; } = new();

        public ManagementDashboardStatsResponseDto? Management { get; init; }
    }

    private sealed class PersonalDashboardStatsResponseDto
    {
        public int MyClubCount { get; init; }

        public int MyPendingApplicationCount { get; init; }

        public int MyUpcomingEventCount { get; init; }
    }

    private sealed class ManagementDashboardStatsResponseDto
    {
        public bool AllClubs { get; init; }

        public int ScopeClubCount { get; init; }

        public int ScopeMemberCount { get; init; }

        public int ScopePendingApplicationCount { get; init; }

        public int ScopeUpcomingEventCount { get; init; }
    }

    private sealed class MyClubMembershipResponseDto
    {
        public int ClubId { get; init; }

        public string ClubName { get; init; } = string.Empty;

        public bool ClubIsActive { get; init; }

        public string ClubRole { get; init; } = string.Empty;

        public DateTime JoinedAtUtc { get; init; }

        public string Relationship { get; init; } = string.Empty;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
