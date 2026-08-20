using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Business.BackgroundJobs;
using DataAccess;
using Entities;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 5 "bitti sayılır": yığının her parçası (aspect, transaction, audit,
/// kuyruk, refresh) tek akışta çalışıyor. DataGrid frontend-only olduğu için burada değil,
/// manuel tarayıcı testinde kanıtlanır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class MembershipVerticalSliceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdvisorEmail = "vs-advisor@test.local";
    private const string AdvisorPassword = "Advisor!Test123456";
    private const string OtherAdvisorEmail = "vs-other-advisor@test.local";
    private const string OtherAdvisorPassword = "OtherAdvisor!Test123456";
    private const string StudentEmail = "vs-student@test.local";
    private const string StudentPassword = "Student!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _clubId;

    public MembershipVerticalSliceTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var advisorUser = await EnsureUserAsync(userManager, AdvisorEmail, AdvisorPassword, "Advisor");
        await EnsureUserAsync(userManager, OtherAdvisorEmail, OtherAdvisorPassword, "Advisor");
        var studentUser = await EnsureUserAsync(userManager, StudentEmail, StudentPassword, "Member");

        var department = await db.Departments.FirstAsync();

        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == advisorUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == studentUser.Id);
        if (student is null)
        {
            student = new Student
            {
                ApplicationUserId = studentUser.Id,
                StudentNumber = $"VS{Guid.NewGuid():N}"[..12],
                DepartmentId = department.Id,
                EnrollmentYear = 2026,
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        var club = new Club
        {
            Name = $"Dikey Dilim Kulübü {Guid.NewGuid():N}",
            AdvisorId = advisor.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        _clubId = club.Id;

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Dikey dilim: giriş → refresh → kulüp listesi → başvuru → 403 (yanlış danışman) → onay → üyelik + audit + kuyruk")]
    public async Task FullVerticalSlice_WorksEndToEnd()
    {
        // 1. Öğrenci girişi.
        var studentLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = StudentEmail, Password = StudentPassword });
        Assert.Equal(HttpStatusCode.OK, studentLoginResponse.StatusCode);

        // Canlı refresh — Faz 3'ün oturum yenileme akışının bu dikey dilimde de çalıştığının kanıtı.
        var studentToken = await RefreshAndGetAccessTokenAsync(studentLoginResponse);

        // 2. Kulüp listesi.
        var clubsResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", studentToken);
        Assert.Equal(HttpStatusCode.OK, clubsResponse.StatusCode);
        Assert.Contains($"\"id\":{_clubId}", await clubsResponse.Content.ReadAsStringAsync());

        // 3. Başvuru.
        var applyResponse = await SendWithBearerAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/membership-applications", studentToken);
        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        // Aynı başvuruyu tekrarlamak Conflict döner (A-15'in üyelik/başvuru tarafındaki iş kuralı yansıması).
        var duplicateResponse = await SendWithBearerAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/membership-applications", studentToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.MembershipApplications.SingleAsync(a => a.ClubId == _clubId)).Id;
        }

        // 4. memberships.write iznine sahip AMA bu kulübün danışmanı OLMAYAN kullanıcı onay denemesi
        // — Y-23'ün "izin claim'i kaynak sahipliği değildir" kuralının doğrudan kanıtı.
        var otherAdvisorLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = OtherAdvisorEmail, Password = OtherAdvisorPassword });
        var otherAdvisorToken = (await otherAdvisorLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;

        var wrongAdvisorReview = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/membership-applications/{applicationId}/decision", otherAdvisorToken, new { Status = "Approved" });
        Assert.Equal(HttpStatusCode.Forbidden, wrongAdvisorReview.StatusCode);

        // 5. Gerçek danışman girişi.
        var advisorLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdvisorEmail, Password = AdvisorPassword });
        var advisorToken = (await advisorLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;

        // 6. Bekleyen başvurular listesinde görünüyor mu?
        var pendingResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/membership-applications", advisorToken);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);
        Assert.Contains($"\"id\":{applicationId}", await pendingResponse.Content.ReadAsStringAsync());

        // 7. Onayla — MembershipApplicationManager.ReviewAsync'in elle yönettiği transaction (Y-46/Y-06).
        var reviewResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/membership-applications/{applicationId}/decision", advisorToken, new { Status = "Approved" });
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 8. Transaction kanıtı: ClubMembership atomik olarak oluşturuldu.
            Assert.True(await db.ClubMemberships.AnyAsync(m => m.ClubId == _clubId));

            // Audit kanıtı (K-12): başvuru güncellemesi denetim izine yazıldı.
            Assert.NotEmpty(await db.AuditLogs
                .Where(a => a.EntityType == nameof(MembershipApplication) && a.EntityId == applicationId.ToString())
                .ToListAsync());
        }

        // 9. Kuyruk kanıtı: bildirim işi Hangfire'da Succeeded durumuna ulaşana kadar bekle.
        await WaitForNotificationJobToSucceedAsync(TimeSpan.FromSeconds(10));

        Assert.Contains(_factory.EmailSender.SentEmails, e => e.ToAddress == StudentEmail);
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
            if (succeeded.Any(j => j.Value?.Job?.Type == typeof(MembershipDecisionNotificationJob)))
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Hangfire bildirim işi zaman aşımı içinde Succeeded durumuna ulaşmadı.");
    }

    private async Task<string> RefreshAndGetAccessTokenAsync(HttpResponseMessage loginResponse)
    {
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var refreshCookie = ExtractCookie(loginResponse, "RefreshToken");
        var antiforgeryCookie = ExtractCookie(loginResponse, "__Host-Csrf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"RefreshToken={refreshCookie}; __Host-Csrf={antiforgeryCookie}");
        request.Headers.Add("X-XSRF-TOKEN", loginBody!.CsrfToken);

        var response = await _client.SendAsync(request);
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

    private static string ExtractCookie(HttpResponseMessage response, string cookieName)
    {
        var setCookieHeader = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith($"{cookieName}=", StringComparison.Ordinal));

        var value = setCookieHeader[(cookieName.Length + 1)..];
        var separatorIndex = value.IndexOf(';');
        return separatorIndex >= 0 ? value[..separatorIndex] : value;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
