using System.Net;
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
/// docs/PLAN-V2.md · Faz 14'ün kabul testi: token HİÇ gönderilmeden üç /api/public/* ucu 200 dönüyor;
/// cevap gövdesinde seed edilen danışman/öğrenci e-postası ve öğrenci numarası GEÇMİYOR; Members
/// görünürlüklü duyuru, Draft/PendingApproval etkinlik ve IsActive=false kulüp hiç YOK (Y-58).
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class PublicSurfaceLeakTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PublicSurfaceLeakTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Anonim ziyaretçi: /api/public/clubs yalnızca aktif kulübü döner, pasif kulüp ve danışman bilgisi sızmaz")]
    public async Task GetPublicClubs_Anonymous_ReturnsOnlyActiveClubWithoutAdvisorInfo()
    {
        var scenario = await SeedScenarioAsync("clubs");

        var response = await _client.GetAsync("/api/public/clubs?pageIndex=0&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(scenario.ActiveClubName, body);
        Assert.DoesNotContain(scenario.InactiveClubName, body);
        AssertNoPii(body, scenario);
    }

    [Fact(DisplayName = "Anonim ziyaretçi: /api/public/events yalnızca yayındaki gelecek etkinliği döner, Draft/PendingApproval sızmaz")]
    public async Task GetPublicEvents_Anonymous_ReturnsOnlyPublishedFutureEvent()
    {
        var scenario = await SeedScenarioAsync("events");

        var response = await _client.GetAsync("/api/public/events?pageIndex=0&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(scenario.PublishedEventTitle, body);
        Assert.DoesNotContain(scenario.DraftEventTitle, body);
        Assert.DoesNotContain(scenario.PendingEventTitle, body);
        AssertNoPii(body, scenario);
    }

    [Fact(DisplayName = "Anonim ziyaretçi: /api/public/announcements yalnızca Public görünürlüklü duyuruyu döner, Members duyurusu sızmaz")]
    public async Task GetPublicAnnouncements_Anonymous_ReturnsOnlyPublicVisibilityAnnouncement()
    {
        var scenario = await SeedScenarioAsync("announcements");

        var response = await _client.GetAsync("/api/public/announcements?pageIndex=0&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(scenario.PublicAnnouncementTitle, body);
        Assert.DoesNotContain(scenario.MembersOnlyAnnouncementTitle, body);
        AssertNoPii(body, scenario);
    }

    [Fact(DisplayName = "Anonim ziyaretçi: /api/public/stats yalnızca sayılar döner, e-posta ve öğrenci no sızmaz")]
    public async Task GetPublicStats_Anonymous_ReturnsCountsWithoutPii()
    {
        var scenario = await SeedScenarioAsync("stats");

        var response = await _client.GetAsync("/api/public/stats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("clubCount", body, StringComparison.Ordinal);
        Assert.Contains("activeClubCount", body, StringComparison.Ordinal);
        Assert.Contains("studentCount", body, StringComparison.Ordinal);
        Assert.Contains("upcomingEventCount", body, StringComparison.Ordinal);
        Assert.DoesNotContain(scenario.ActiveClubName, body);
        Assert.DoesNotContain(scenario.PublishedEventTitle, body);
        AssertNoPii(body, scenario);
    }

    private static void AssertNoPii(string body, Scenario scenario)
    {
        Assert.DoesNotContain(scenario.AdvisorEmail, body);
        Assert.DoesNotContain(scenario.StudentEmail, body);
        Assert.DoesNotContain(scenario.StudentNumber, body);
    }

    private sealed record Scenario(
        string ActiveClubName,
        string InactiveClubName,
        string PublishedEventTitle,
        string DraftEventTitle,
        string PendingEventTitle,
        string PublicAnnouncementTitle,
        string MembersOnlyAnnouncementTitle,
        string AdvisorEmail,
        string StudentEmail,
        string StudentNumber);

    private async Task<Scenario> SeedScenarioAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var faculty = new Faculty { Name = $"pub-leak-fac-{suffix}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();
        var department = new Department { Name = $"pub-leak-dept-{suffix}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var advisorEmail = $"pub-leak-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, "Advisor!Test123456");
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");
        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var studentEmail = $"pub-leak-student-{suffix}@test.local";
        var studentNumber = $"S{Guid.NewGuid():N}"[..12];
        var studentUser = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
        var studentCreateResult = await userManager.CreateAsync(studentUser, "Student!Test123456");
        Assert.True(studentCreateResult.Succeeded, string.Join("; ", studentCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(studentUser, "Member");
        db.Students.Add(new Student { ApplicationUserId = studentUser.Id, StudentNumber = studentNumber, DepartmentId = department.Id, EnrollmentYear = 2026 });
        await db.SaveChangesAsync();

        var activeClubName = $"pub-leak-active-club-{suffix}";
        var activeClub = new Club { Name = activeClubName, AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(activeClub);
        var inactiveClubName = $"pub-leak-inactive-club-{suffix}";
        db.Clubs.Add(new Club { Name = inactiveClubName, AdvisorId = advisor.Id, IsActive = false, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var publishedTitle = $"pub-leak-published-event-{suffix}";
        db.Events.Add(new Event
        {
            ClubId = activeClub.Id, Title = publishedTitle, StartDateUtc = DateTime.UtcNow.AddDays(3), EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
            Status = EventStatus.Published, CreatedAtUtc = DateTime.UtcNow,
        });
        var draftTitle = $"pub-leak-draft-event-{suffix}";
        db.Events.Add(new Event
        {
            ClubId = activeClub.Id, Title = draftTitle, StartDateUtc = DateTime.UtcNow.AddDays(3), EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
            Status = EventStatus.Draft, CreatedAtUtc = DateTime.UtcNow,
        });
        var pendingTitle = $"pub-leak-pending-event-{suffix}";
        db.Events.Add(new Event
        {
            ClubId = activeClub.Id, Title = pendingTitle, StartDateUtc = DateTime.UtcNow.AddDays(3), EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
            Status = EventStatus.PendingApproval, CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var publicAnnouncementTitle = $"pub-leak-public-announcement-{suffix}";
        db.Announcements.Add(new Announcement
        {
            ClubId = activeClub.Id, Title = publicAnnouncementTitle, Content = "Herkese açık içerik.", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = DateTime.UtcNow,
        });
        var membersOnlyTitle = $"pub-leak-members-announcement-{suffix}";
        db.Announcements.Add(new Announcement
        {
            ClubId = activeClub.Id, Title = membersOnlyTitle, Content = "Yalnızca üyelere özel içerik.", Visibility = AnnouncementVisibility.Members, PublishedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return new Scenario(
            activeClubName, inactiveClubName, publishedTitle, draftTitle, pendingTitle,
            publicAnnouncementTitle, membersOnlyTitle, advisorEmail, studentEmail, studentNumber);
    }

    [Fact(DisplayName = "Y-72: /api/public/events ucundan ClubMembers kitleli etkinlik donmez, Public doner")]
    public async Task GetPublicEvents_Anonymous_ExcludesClubMembersAudience()
    {
        // Y-34: test kendi verisini kurar. Danışmanı "başka bir test yaratmıştır" diye varsaymak
        // testi calistirma sirasina bagimli kilardi; SeedScenarioAsync fakulte/bolum/danisman/
        // ogrenci/kulup/etkinlik zincirinin tamamini kendisi uretir.
        var suffix = $"aud{Guid.NewGuid():N}"[..11];
        var scenario = await SeedScenarioAsync(suffix);
        var membersOnlyTitle = $"pub-leak-members-event-{suffix}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var club = await db.Clubs.SingleAsync(c => c.Name == scenario.ActiveClubName);

            db.Events.Add(new Event
            {
                ClubId = club.Id,
                Title = membersOnlyTitle,
                StartDateUtc = DateTime.UtcNow.AddDays(3),
                EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
                Status = EventStatus.Published,
                Audience = EventAudience.ClubMembers,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // A-50: arama sunucuda. `search` olmadan pageSize=200 istemek ise yaramaz — sunucu 100'e
        // kirpiyor (Y-11) ve liste StartDateUtc'ye gore artan sirali oldugu icin bu testin
        // etkinlikleri birikmis kayitlarin arkasinda kalirdi. O halde DoesNotContain YANLIS
        // SEBEPTEN gecerdi; asagidaki Contains muhafizi bunu yakalar.
        var response = await _client.GetAsync($"/api/public/events?pageIndex=0&pageSize=100&search={suffix}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Seed'in yayindaki etkinligi Audience varsayilani (Public) ile gelir ve GORUNMELI.
        // Bu satir kasitli: filtre "her seyi ele" haline gelirse kirmiziya doner ve testin
        // yalnizca bos liste gordugu icin gecmesini engeller.
        Assert.Contains(scenario.PublishedEventTitle, body, StringComparison.Ordinal);
        Assert.DoesNotContain(membersOnlyTitle, body, StringComparison.Ordinal);
    }
}
