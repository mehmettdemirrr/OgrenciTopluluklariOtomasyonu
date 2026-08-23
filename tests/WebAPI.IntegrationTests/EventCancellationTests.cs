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
/// docs/PLAN-V4.md §20 "Çıkış koşulu" (A-49/Y-61): yayındaki etkinlik iptal ediliyor, anonim vitrinden
/// düşüyor, yeni kayıt `Conflict` veriyor, katılımcı kayıtları **silinmiyor**, katılımcılara e-posta gidiyor.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class EventCancellationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdvisorEmail = "ec-advisor@test.local";
    private const string AdvisorPassword = "Advisor!Test123456";
    private const string StudentEmail = "ec-student@test.local";
    private const string StudentPassword = "Student!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _clubId;
    private int _studentId;

    public EventCancellationTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var advisorUser = await EnsureUserAsync(userManager, AdvisorEmail, AdvisorPassword, "Advisor");
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
                StudentNumber = $"EC{Guid.NewGuid():N}"[..12],
                DepartmentId = department.Id,
                EnrollmentYear = 2026,
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        _studentId = student.Id;

        var club = new Club
        {
            Name = $"İptal Kulübü {Guid.NewGuid():N}"[..28],
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

    [Fact(DisplayName = "Y-61: yayındaki etkinlik iptal edilir → vitrinden düşer, yeni kayıt Conflict, mevcut kayıt SİLİNMEZ, e-posta gider")]
    public async Task Cancel_RemovesFromShowcase_BlocksRegistration_ButKeepsParticipations()
    {
        var eventId = await CreatePublishedEventAsync();
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        // 1. Öğrenci kaydolur.
        var registerResponse = await SendAsync(HttpMethod.Post, $"/api/events/{eventId}/participation", studentToken);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // 2. İptalden ÖNCE anonim vitrinde görünüyor (token yok — Y-58 yüzeyi).
        var beforeShowcase = await _client.GetStringAsync("/api/public/events?pageIndex=0&pageSize=100");
        Assert.Contains($"\"id\":{eventId}", beforeShowcase);

        // 3. Danışman iptal eder.
        var advisorToken = await LoginAsync(AdvisorEmail, AdvisorPassword);
        var cancelResponse = await SendAsync(
            HttpMethod.Put, $"/api/events/{eventId}/cancellation", advisorToken, new { CancellationReason = "Salon tahsisi iptal edildi." });
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var @event = await db.Events.SingleAsync(e => e.Id == eventId);

            Assert.Equal(EventStatus.Cancelled, @event.Status);
            Assert.Equal("Salon tahsisi iptal edildi.", @event.CancellationReason);

            // A-49'un kalbi: kayıt SİLİNMEZ — öğrenci kaydını iptal rozetiyle görmeye devam eder.
            Assert.True(await db.EventParticipations.AnyAsync(p => p.EventId == eventId && p.StudentId == _studentId));
        }

        // 4. Y-61: anonim vitrinden düştü.
        var afterShowcase = await _client.GetStringAsync("/api/public/events?pageIndex=0&pageSize=100");
        Assert.DoesNotContain($"\"id\":{eventId}", afterShowcase);

        // 5. Y-61: yeni kayıt kabul edilmiyor.
        var reRegisterResponse = await SendAsync(HttpMethod.Post, $"/api/events/{eventId}/participation", studentToken);
        Assert.Equal(HttpStatusCode.Conflict, reRegisterResponse.StatusCode);

        // 6. Öğrenci "Etkinliklerim"de hâlâ görüyor — durum filtresi yok, kasıtlı (A-49).
        var mineResponse = await SendAsync(HttpMethod.Get, "/api/events/mine?pageIndex=0&pageSize=100", studentToken);
        var mineBody = await mineResponse.Content.ReadAsStringAsync();
        Assert.Contains($"\"id\":{eventId}", mineBody);
        Assert.Contains("\"status\":\"Cancelled\"", mineBody);

        // 7. O-7: katılımcıya e-posta gitti.
        await WaitForCancellationJobAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(_factory.EmailSender.SentEmails, e => e.ToAddress == StudentEmail);
    }

    [Fact(DisplayName = "A-49: yalnızca Published iptal edilebilir — Draft iptal denemesi Conflict")]
    public async Task Cancel_DraftEvent_ReturnsConflict()
    {
        var advisorToken = await LoginAsync(AdvisorEmail, AdvisorPassword);

        var createResponse = await SendAsync(
            HttpMethod.Post, $"/api/clubs/{_clubId}/events", advisorToken,
            new
            {
                Title = "Taslak Etkinlik",
                StartDateUtc = DateTime.UtcNow.AddDays(3),
                EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
            });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        int draftEventId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            draftEventId = (await db.Events.Where(e => e.Title == "Taslak Etkinlik").OrderByDescending(e => e.Id).FirstAsync()).Id;
        }

        var cancelResponse = await SendAsync(
            HttpMethod.Put, $"/api/events/{draftEventId}/cancellation", advisorToken, new { CancellationReason = "Gerekçe" });
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);

        // Regresyon: Draft hâlâ silinebiliyor (Y-16 soft delete yolu bozulmadı).
        var deleteResponse = await SendAsync(HttpMethod.Delete, $"/api/events/{draftEventId}", advisorToken);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact(DisplayName = "Y-35: iptal gerekçesi zorunlu — boş gerekçe doğrulama hatası")]
    public async Task Cancel_WithoutReason_ReturnsValidationError()
    {
        var eventId = await CreatePublishedEventAsync();
        var advisorToken = await LoginAsync(AdvisorEmail, AdvisorPassword);

        var response = await SendAsync(
            HttpMethod.Put, $"/api/events/{eventId}/cancellation", advisorToken, new { CancellationReason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CreatePublishedEventAsync()
    {
        var advisorToken = await LoginAsync(AdvisorEmail, AdvisorPassword);
        var title = $"İptal Testi {Guid.NewGuid():N}"[..25];

        var createResponse = await SendAsync(
            HttpMethod.Post, $"/api/clubs/{_clubId}/events", advisorToken,
            new
            {
                Title = title,
                StartDateUtc = DateTime.UtcNow.AddDays(5),
                EndDateUtc = DateTime.UtcNow.AddDays(5).AddHours(2),
            });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        int eventId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            eventId = (await db.Events.SingleAsync(e => e.Title == title)).Id;
        }

        await SendAsync(HttpMethod.Put, $"/api/events/{eventId}/submission", advisorToken);
        var decideResponse = await SendAsync(
            HttpMethod.Put, $"/api/events/{eventId}/decision", advisorToken, new { Status = "Published" });
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);

        return eventId;
    }

    private async Task WaitForCancellationJobAsync(TimeSpan timeout)
    {
        using var scope = _factory.Services.CreateScope();
        var monitoringApi = scope.ServiceProvider.GetRequiredService<JobStorage>().GetMonitoringApi();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (monitoringApi.SucceededJobs(0, 100).Any(j => j.Value?.Job?.Type == typeof(EventCancellationNotificationJob)))
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Hangfire iptal bildirimi işi zaman aşımı içinde Succeeded durumuna ulaşmadı.");
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
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
