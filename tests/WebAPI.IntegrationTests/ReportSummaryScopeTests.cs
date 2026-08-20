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

/// <summary>docs/MIMARI.md · A-35 × A-10: rapor kapsamı izin claim'ine göre farklı sonuç üretmeli.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class ReportSummaryScopeTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ReportSummaryScopeTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Danışmanın özeti yalnızca kendi danışmanı olduğu kulübü sayar (A-35×A-10)")]
    public async Task GetSummary_Advisor_CountsOnlyOwnClub()
    {
        var scenario = await SeedAdvisorClubWithMemberAsync("adv");
        var token = await LoginAndGetAccessTokenAsync(scenario.Email, scenario.Password);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/reports/summary", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = await response.Content.ReadFromJsonAsync<List<TermSummaryRowResponseDto>>();
        var row = rows!.Single(r => r.AcademicTermId == scenario.TermId);

        // Başka danışmanların/senaryoların kulüpleri de aynı veritabanında olabilir (IClassFixture
        // fact'ler arasında paylaşılır) — kapsam bunlardan tamamen izole olmalı, tam olarak 1.
        Assert.Equal(1, row.ClubCount);
        Assert.Equal(1, row.MemberCount);
    }

    [Fact(DisplayName = "reports.read.all izni olan admin tüm kulüpleri görür (A-35×A-10)")]
    public async Task GetSummary_AdminWithReadAll_MatchesDatabaseGroundTruth()
    {
        await SeedAdvisorClubWithMemberAsync("admin-scope-1");
        var scenario2 = await SeedAdvisorClubWithMemberAsync("admin-scope-2");

        const string adminEmail = "rpt-summary-admin@test.local";
        const string adminPassword = "Admin!Test123456";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await EnsureUserAsync(userManager, adminEmail, adminPassword, "Admin");
        }

        var adminToken = await LoginAndGetAccessTokenAsync(adminEmail, adminPassword);
        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/reports/summary", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = await response.Content.ReadFromJsonAsync<List<TermSummaryRowResponseDto>>();
        var row = rows!.Single(r => r.AcademicTermId == scenario2.TermId);

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var expectedClubCount = await db.ClubMemberships.Where(m => m.AcademicTermId == scenario2.TermId).Select(m => m.ClubId).Distinct().CountAsync();
        var expectedMemberCount = await db.ClubMemberships.CountAsync(m => m.AcademicTermId == scenario2.TermId);

        Assert.Equal(expectedClubCount, row.ClubCount);
        Assert.Equal(expectedMemberCount, row.MemberCount);
        Assert.True(row.ClubCount >= 2, "Admin (reports.read.all) en az iki farklı danışmanın kulübünü görmeli — danışmanın kapsamıyla sınırlı kalmamalı.");
    }

    private sealed record AdvisorScenario(string Email, string Password, int ClubId, int TermId);

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

        // IX_AcademicTerms_IsCurrent: yalnızca bir "güncel" dönem olabilir, fact'ler arasında paylaşılır.
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
        var advisorEmail = $"rpt-summary-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Özet Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var studentEmail = $"rpt-summary-student-{suffix}@test.local";
        var studentUser = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
        var studentCreateResult = await userManager.CreateAsync(studentUser, "Student!Test123456");
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

        return new AdvisorScenario(advisorEmail, advisorPassword, club.Id, term.Id);
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

    private sealed class TermSummaryRowResponseDto
    {
        public int AcademicTermId { get; init; }

        public string TermName { get; init; } = string.Empty;

        public int ClubCount { get; init; }

        public int MemberCount { get; init; }

        public int EventCount { get; init; }
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
