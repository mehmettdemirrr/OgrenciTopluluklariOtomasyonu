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
/// docs/MIMARI.md · Faz 7 §0.5: Draft → PendingApproval → Published uçtan uca; Y-23'ün "izin
/// claim'i kaynak sahipliği değildir" kuralı burada da (MembershipApplicationManager precedent'i
/// gibi) hem oluşturma/gönderme hem karar tarafında ayrı ayrı kanıtlanır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class EventApprovalFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public EventApprovalFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Danışman: oluştur → submit → onay kuyruğunda görün → onayla → ikinci onay 409")]
    public async Task FullFlow_CreateSubmitApprove_WorksEndToEnd()
    {
        var scenario = await SeedScenarioAsync("flow");
        var officerToken = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);

        var eventId = await CreateEventAsync(officerToken, scenario.Club.Id);

        var submitResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/events/{eventId}/submission", officerToken);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var queueResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/events/approval-queue", advisorToken);
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Contains($"\"id\":{eventId}", await queueResponse.Content.ReadAsStringAsync());

        var decideResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/events/{eventId}/decision", advisorToken, new { Status = "Published" });
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var @event = await db.Events.SingleAsync(e => e.Id == eventId);
            Assert.Equal(EventStatus.Published, @event.Status);
        }

        var secondDecide = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/events/{eventId}/decision", advisorToken, new { Status = "Published" });
        Assert.Equal(HttpStatusCode.Conflict, secondDecide.StatusCode);
    }

    [Fact(DisplayName = "events.write izni olmayan düz üye etkinlik oluşturamaz")]
    public async Task Create_PlainMember_ReturnsForbidden()
    {
        var scenario = await SeedScenarioAsync("plain");
        var memberToken = await LoginAndGetAccessTokenAsync(scenario.PlainMemberEmail, scenario.PlainMemberPassword);

        var response = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.Club.Id}/events", memberToken,
            new { Title = "Deneme", StartDateUtc = DateTime.UtcNow.AddDays(5), EndDateUtc = DateTime.UtcNow.AddDays(5).AddHours(2) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Başka kulübün yetkilisi bu kulüpte etkinlik oluşturamaz")]
    public async Task Create_OfficerOfAnotherClub_ReturnsForbidden()
    {
        var scenarioA = await SeedScenarioAsync("clubA");
        var scenarioB = await SeedScenarioAsync("clubB");

        var officerBToken = await LoginAndGetAccessTokenAsync(scenarioB.OfficerEmail, scenarioB.OfficerPassword);

        var response = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenarioA.Club.Id}/events", officerBToken,
            new { Title = "Deneme", StartDateUtc = DateTime.UtcNow.AddDays(5), EndDateUtc = DateTime.UtcNow.AddDays(5).AddHours(2) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreateEventAsync(string officerToken, int clubId)
    {
        var response = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{clubId}/events", officerToken,
            new { Title = "Yılsonu Etkinliği", StartDateUtc = DateTime.UtcNow.AddDays(10), EndDateUtc = DateTime.UtcNow.AddDays(10).AddHours(3) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<int>();
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

    private sealed record Scenario(
        Club Club, string AdvisorEmail, string AdvisorPassword, string OfficerEmail, string OfficerPassword,
        string PlainMemberEmail, string PlainMemberPassword);

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

        // IX_AcademicTerms_IsCurrent: bu sınıftaki tüm fact'ler AYNI factory/veritabanını paylaşır
        // (IClassFixture) — sonraki senaryolar ilk senaryonun oluşturduğu güncel dönemi yeniden kullanır.
        var term = await db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
        if (term is null)
        {
            term = new AcademicTerm { Name = $"2026-Güz-{suffix}", StartDateUtc = DateTime.UtcNow.AddMonths(-1), EndDateUtc = DateTime.UtcNow.AddMonths(3), IsCurrent = true };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();
        }

        const string advisorPassword = "Advisor!Test123456";
        var advisorEmail = $"evt-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Etkinlik Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        // ClubOfficer rolü token'a events.write claim'ini taşır; DB'deki ClubMembership.ClubRole=Officer
        // ise Y-23'ün ikinci (kaynak sahipliği) katmanını sağlar.
        const string officerPassword = "Officer!Test123456";
        var officerEmail = $"evt-officer-{suffix}@test.local";
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
        var plainMemberEmail = $"evt-member-{suffix}@test.local";
        var plainMemberUser = new ApplicationUser { UserName = plainMemberEmail, Email = plainMemberEmail, EmailConfirmed = true };
        var plainMemberCreateResult = await userManager.CreateAsync(plainMemberUser, plainMemberPassword);
        Assert.True(plainMemberCreateResult.Succeeded, string.Join("; ", plainMemberCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(plainMemberUser, "Member");

        return new Scenario(club, advisorEmail, advisorPassword, officerEmail, officerPassword, plainMemberEmail, plainMemberPassword);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
