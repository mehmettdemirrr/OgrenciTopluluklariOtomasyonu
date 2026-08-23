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
/// docs/PLAN-V3.md §17 "Çıkış koşulu": öğrenci başvurur → admin kuyruğunda görür → onaylar →
/// kulüp oluşur, öğrenci o kulübün President'i olur → yeni kulüp anında listede görünür (cache) →
/// aynı dönemde ikinci bekleyen başvuru DB seviyesinde reddedilir.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class ClubApplicationFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string StudentEmail = "ca-student@test.local";
    private const string StudentPassword = "Student!Test123456";
    private const string OtherStudentEmail = "ca-other-student@test.local";
    private const string OtherStudentPassword = "OtherStudent!Test123456";
    private const string AdvisorEmail = "ca-advisor@test.local";
    private const string AdvisorPassword = "Advisor!Test123456";
    private const string AdminEmail = "ca-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string MemberEmail = "ca-member@test.local";
    private const string MemberPassword = "Member!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _proposedAdvisorId;

    public ClubApplicationFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(userManager, StudentEmail, StudentPassword, "Member");
        await EnsureUserAsync(userManager, OtherStudentEmail, OtherStudentPassword, "Member");
        await EnsureUserAsync(userManager, MemberEmail, MemberPassword, "Member");
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Admin");
        var advisorUser = await EnsureUserAsync(userManager, AdvisorEmail, AdvisorPassword, "Advisor");

        var department = await db.Departments.FirstAsync();

        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == advisorUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        _proposedAdvisorId = advisor.Id;

        foreach (var (email, number) in new[] { (StudentEmail, "CA-S1"), (OtherStudentEmail, "CA-S2"), (MemberEmail, "CA-S3") })
        {
            var user = await userManager.FindByEmailAsync(email);
            var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user!.Id);
            if (student is null)
            {
                db.Students.Add(new Student
                {
                    ApplicationUserId = user!.Id,
                    StudentNumber = $"{number}{Guid.NewGuid():N}"[..12],
                    DepartmentId = department.Id,
                    EnrollmentYear = 2026,
                });
                await db.SaveChangesAsync();
            }
        }

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Onay akışı: başvuru → çift bekleyen başvuru reddedilir → yetkisiz karar 403 → onay → kulüp+President+audit+bildirim+cache")]
    public async Task Submit_Then_Approve_CreatesClubAndPresidentMembership()
    {
        var proposedName = $"Robotik Kulübü {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        // 1. Kulüp önce vitrinde/listede yok.
        var beforeResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs?pageIndex=0&pageSize=200", studentToken);
        Assert.DoesNotContain(proposedName, await beforeResponse.Content.ReadAsStringAsync());

        // 2. Başvuru.
        var submitResponse = await SendWithBearerAsync(
            HttpMethod.Post, "/api/club-applications", studentToken,
            new { ProposedName = proposedName, Description = "Test açıklaması", Justification = "Test gerekçesi", ProposedAdvisorId = _proposedAdvisorId });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        // 3. Aynı dönemde ikinci bekleyen başvuru — filtreli unique index'in iş kuralı yansıması (A-45).
        var duplicateResponse = await SendWithBearerAsync(
            HttpMethod.Post, "/api/club-applications", studentToken,
            new { ProposedName = $"Başka Ad {Guid.NewGuid():N}"[..20], Justification = "Başka gerekçe", ProposedAdvisorId = _proposedAdvisorId });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        // 4. "Başvurularım" ekranında görünüyor mu?
        var mineResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications/mine", studentToken);
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        Assert.Contains(proposedName, await mineResponse.Content.ReadAsStringAsync());

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName)).Id;
        }

        // 5. clubs.write taşımayan kimliği doğrulanmış kullanıcı (sıradan öğrenci) karar veremez.
        var memberToken = await LoginAsync(MemberEmail, MemberPassword);
        var unauthorizedDecision = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", memberToken, new { Status = "Approved" });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedDecision.StatusCode);

        // 6. Admin kuyrukta görür.
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var pendingResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications", adminToken);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);
        Assert.Contains(proposedName, await pendingResponse.Content.ReadAsStringAsync());

        // 7. Onayla — ClubApplicationManager.DecideAsync'in elle yönettiği transaction (Y-46/Y-06).
        var decideResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken, new { Status = "Approved", ReviewNote = "Uygun bulundu." });
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);

        int createdClubId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 8. Club oluştu, başvuru CreatedClubId'yi taşıyor.
            var application = await db.ClubApplications.SingleAsync(a => a.Id == applicationId);
            Assert.Equal(ApplicationStatus.Approved, application.Status);
            Assert.NotNull(application.CreatedClubId);
            createdClubId = application.CreatedClubId!.Value;

            var club = await db.Clubs.SingleAsync(c => c.Id == createdClubId);
            Assert.Equal(proposedName, club.Name);
            Assert.True(club.IsActive);

            // 9. Başvuran öğrenci o kulübün President'i oldu (O-3).
            var membership = await db.ClubMemberships.SingleAsync(m => m.ClubId == createdClubId);
            Assert.Equal(ClubRole.President, membership.ClubRole);

            // Audit kanıtı (K-12).
            Assert.NotEmpty(await db.AuditLogs
                .Where(a => a.EntityType == nameof(ClubApplication) && a.EntityId == applicationId.ToString())
                .ToListAsync());
        }

        // 10. Cache geçersizleştirme kanıtı: yeni kulüp anında listede (Y-45'in ClubManager ayağı).
        var afterResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs?pageIndex=0&pageSize=200", studentToken);
        Assert.Contains(proposedName, await afterResponse.Content.ReadAsStringAsync());

        // 11. Kuyruk kanıtı: bildirim işi Hangfire'da Succeeded durumuna ulaşana kadar bekle.
        await WaitForNotificationJobToSucceedAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(_factory.EmailSender.SentEmails, e => e.ToAddress == StudentEmail);

        // Not: yeni başkan hâlâ "Member" Identity rolünde — ClubRole=President tek başına
        // events.write JWT claim'ini taşımaz (bkz. EventApprovalFlowTests: officer'a ayrıca
        // "ClubOfficer" Identity rolü atanıyor). Onay akışı bir Identity rol ataması yapmaz;
        // bu bilinçli bir sınır, ayrı bir admin adımı gerektirir (Faz 17 kapsamı dışında).
    }

    [Fact(DisplayName = "Ret akışı: başvuru → reddet → kulüp oluşmaz, ret bildirimi gönderilir")]
    public async Task Submit_Then_Reject_CreatesNoClub()
    {
        var proposedName = $"Satranç Kulübü {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var submitResponse = await SendWithBearerAsync(
            HttpMethod.Post, "/api/club-applications", studentToken,
            new { ProposedName = proposedName, Justification = "Test gerekçesi", ProposedAdvisorId = _proposedAdvisorId });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName)).Id;
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var decideResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Rejected", ReviewNote = "Yeterli üye ilgisi yok." });
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.Id == applicationId);
            Assert.Equal(ApplicationStatus.Rejected, application.Status);
            Assert.Null(application.CreatedClubId);
            Assert.False(await db.Clubs.AnyAsync(c => c.Name == proposedName));
        }

        // Aynı başvuru için ikinci bir karar reddedilir (idempotentlik — MembershipApplicationManager precedent'i).
        var secondDecision = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken, new { Status = "Approved" });
        Assert.Equal(HttpStatusCode.Conflict, secondDecision.StatusCode);

        await WaitForNotificationJobToSucceedAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(_factory.EmailSender.SentEmails, e => e.ToAddress == OtherStudentEmail);
    }

    private async Task WaitForNotificationJobToSucceedAsync(TimeSpan timeout)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<JobStorage>();
        var monitoringApi = storage.GetMonitoringApi();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var succeeded = monitoringApi.SucceededJobs(0, 100);
            if (succeeded.Any(j => j.Value?.Job?.Type == typeof(ClubApplicationDecisionNotificationJob)))
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Hangfire bildirim işi zaman aşımı içinde Succeeded durumuna ulaşmadı.");
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
