using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Business.BackgroundJobs;
using DataAccess;
using Entities;
using Entities.Enums;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 6 "bitti sayılır": yetkisi alınmış kullanıcı kuyruktaki raporunu indiremiyor
/// (Y-51). Üretim ve indirme anlarının ayrı, bağımsız kod yolları olduğunu kanıtlar — kontrol
/// token'ın claim'lerinde değil, indirme anında DB'den yeniden çözülen kapsamda yapılır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class ReportDownloadAuthorizationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ReportDownloadAuthorizationTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Danışman rolü sonradan alınırsa aynı token ile hazır raporu indiremez (Y-51 — Faz 6 bitti sayılır)")]
    public async Task Download_RoleRevokedAfterReady_ReturnsForbiddenWithSameToken()
    {
        var scenario = await SeedScenarioAsync("role");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var reportRequestId = await RequestClubMemberReportAsync(advisorToken, scenario.Club.Id, scenario.AdvisorUser.Id);
        await WaitForReportGenerationSucceededAsync(reportRequestId, TimeSpan.FromSeconds(15));

        var firstDownload = await SendWithBearerAsync(HttpMethod.Get, $"/api/reports/{reportRequestId}/file", advisorToken);
        Assert.Equal(HttpStatusCode.OK, firstDownload.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            firstDownload.Content.Headers.ContentType?.MediaType);

        var bytes = await firstDownload.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 2 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K', "xlsx içeriği ZIP (PK) imzasıyla başlamalı.");

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var advisorUser = await userManager.FindByIdAsync(scenario.AdvisorUser.Id.ToString());
            await userManager.RemoveFromRoleAsync(advisorUser!, "Advisor");
        }

        // Aynı, hâlâ geçerli (15 dk) access token — token'ın kendi claim'leri hâlâ eski izinleri taşıyor,
        // ama DownloadAsync kapsamı DB'den YENİDEN çözer (bkz. ReportManager.DownloadAsync).
        var secondDownload = await SendWithBearerAsync(HttpMethod.Get, $"/api/reports/{reportRequestId}/file", advisorToken);
        Assert.Equal(HttpStatusCode.Forbidden, secondDownload.StatusCode);
    }

    [Fact(DisplayName = "Kulübün danışmanlığı başkasına devredilirse eski danışman aynı token ile hazır raporu indiremez")]
    public async Task Download_AdvisorReassigned_ReturnsForbiddenWithSameToken()
    {
        var scenario = await SeedScenarioAsync("reassign");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var reportRequestId = await RequestClubMemberReportAsync(advisorToken, scenario.Club.Id, scenario.AdvisorUser.Id);
        await WaitForReportGenerationSucceededAsync(reportRequestId, TimeSpan.FromSeconds(15));

        var firstDownload = await SendWithBearerAsync(HttpMethod.Get, $"/api/reports/{reportRequestId}/file", advisorToken);
        Assert.Equal(HttpStatusCode.OK, firstDownload.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var newAdvisorEmail = "rpt-newadvisor-reassign@test.local";
            var newAdvisorUser = new ApplicationUser { UserName = newAdvisorEmail, Email = newAdvisorEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(newAdvisorUser, "NewAdvisor!Test123456");
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(newAdvisorUser, "Advisor");

            var newAdvisor = new AcademicStaff { ApplicationUserId = newAdvisorUser.Id, Title = "Doç.", DepartmentId = scenario.Student.DepartmentId };
            db.AcademicStaff.Add(newAdvisor);
            await db.SaveChangesAsync();

            var club = await db.Clubs.SingleAsync(c => c.Id == scenario.Club.Id);
            club.AdvisorId = newAdvisor.Id;
            await db.SaveChangesAsync();
        }

        var secondDownload = await SendWithBearerAsync(HttpMethod.Get, $"/api/reports/{reportRequestId}/file", advisorToken);
        Assert.Equal(HttpStatusCode.Forbidden, secondDownload.StatusCode);
    }

    [Fact(DisplayName = "Başka kullanıcının raporu indirilemez")]
    public async Task Download_AnotherUsersReport_ReturnsForbidden()
    {
        var scenario = await SeedScenarioAsync("owner");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);
        var reportRequestId = await RequestClubMemberReportAsync(advisorToken, scenario.Club.Id, scenario.AdvisorUser.Id);

        var otherScenario = await SeedScenarioAsync("intruder");
        var otherToken = await LoginAndGetAccessTokenAsync(otherScenario.AdvisorEmail, otherScenario.AdvisorPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/reports/{reportRequestId}/file", otherToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> RequestClubMemberReportAsync(string accessToken, int clubId, int requestedByUserId)
    {
        var response = await SendWithBearerAsync(HttpMethod.Post, "/api/reports", accessToken, new { ReportType = "ClubMembers", ClubId = clubId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reportRequest = await db.ReportRequests
            .Where(r => r.RequestedByUserId == requestedByUserId)
            .OrderByDescending(r => r.Id)
            .FirstAsync();

        return reportRequest.Id;
    }

    private async Task WaitForReportGenerationSucceededAsync(int reportRequestId, TimeSpan timeout)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<JobStorage>();
        var monitoringApi = storage.GetMonitoringApi();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var succeeded = monitoringApi.SucceededJobs(0, 100);
            if (succeeded.Any(j =>
                    j.Value?.Job?.Type == typeof(ReportGenerationJob) &&
                    j.Value.Job.Args.Count > 0 &&
                    Equals(j.Value.Job.Args[0], reportRequestId)))
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Hangfire rapor üretim işi zaman aşımı içinde Succeeded durumuna ulaşmadı.");
    }

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

    private sealed record Scenario(Club Club, ApplicationUser AdvisorUser, string AdvisorEmail, string AdvisorPassword, Student Student);

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

        // IX_AcademicTerms_IsCurrent: yalnızca bir tane "güncel" dönem olabilir — bu test sınıfındaki
        // tüm fact'ler AYNI factory/veritabanını paylaştığı için (IClassFixture), sonraki senaryolar
        // ilkinin oluşturduğu güncel dönemi yeniden kullanır.
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
        var advisorEmail = $"rpt-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Rapor Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var studentEmail = $"rpt-student-{suffix}@test.local";
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

        var membership = new ClubMembership
        {
            ClubId = club.Id, StudentId = student.Id, AcademicTermId = term.Id, ClubRole = ClubRole.Member, JoinedAtUtc = DateTime.UtcNow,
        };
        db.ClubMemberships.Add(membership);
        await db.SaveChangesAsync();

        return new Scenario(club, advisorUser, advisorEmail, advisorPassword, student);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
