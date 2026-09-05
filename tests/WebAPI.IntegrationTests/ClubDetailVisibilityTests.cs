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

/// <summary>docs/MIMARI.md · K-45/A-75/Y-81: üye olmayan öğrenci kulübün yayınlanmış etkinliklerini görür, 403 almaz.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class ClubDetailVisibilityTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ClubDetailVisibilityTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Faz 35'te aynı hata yaşandı: [Flags] enum, yanlış dönüştürücü sırasıyla "MembersView, EventsManage"
    /// gibi metin olarak gitti ve arayüzün bit maskesi sessizce boş kaldı.
    /// </summary>
    [Fact(DisplayName = "ClubDetail: başkanın kapasitesi tel üzerinde sayı gider, ilişki President'tır (Y-81)")]
    public async Task ClubDetail_CapabilitiesTravelAsNumber()
    {
        var scenario = await SeedScenarioAsync("visibility-cap");
        var token = await LoginAndGetAccessTokenAsync(scenario.PresidentEmail, scenario.PresidentPassword);

        var detail = await SendWithBearerAsync(HttpMethod.Get, $"/api/clubs/{scenario.ClubId}", token);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Number, body.GetProperty("myCapabilities").ValueKind);
        Assert.Equal("President", body.GetProperty("myRelationship").GetString());
    }

    [Fact(DisplayName = "Üye olmayan öğrenci yayınlanmış etkinlikleri /api/events?clubId= ucundan görür (K-45)")]
    public async Task PublishedEvents_AreVisible_ToNonMemberStudent()
    {
        var scenario = await SeedScenarioAsync("visibility-published");
        await SeedEventAsync(scenario.ClubId, "Yayında Etkinlik", EventStatus.Published);
        var token = await LoginAndGetAccessTokenAsync(scenario.NonMemberEmail, scenario.NonMemberPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/events?clubId={scenario.ClubId}&pageIndex=0&pageSize=20", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, page.GetProperty("items").GetArrayLength());
    }

    [Fact(DisplayName = "Üye olmayan öğrenci taslak etkinliği göremez (K-45)")]
    public async Task DraftEvents_AreNotVisible_ToNonMemberStudent()
    {
        var scenario = await SeedScenarioAsync("visibility-draft");
        await SeedEventAsync(scenario.ClubId, "Taslak Etkinlik", EventStatus.Draft);
        var token = await LoginAndGetAccessTokenAsync(scenario.NonMemberEmail, scenario.NonMemberPassword);

        var page = await SendWithBearerAsync(HttpMethod.Get, $"/api/events?clubId={scenario.ClubId}&pageIndex=0&pageSize=20", token);
        var body = await page.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Fact(DisplayName = "Üye olmayan öğrenci üye listesi ucunda Forbidden almaya devam eder (Y-81: kapı gevşetilmez)")]
    public async Task Members_StayForbidden_ForNonMemberStudent()
    {
        var scenario = await SeedScenarioAsync("visibility-members");
        var token = await LoginAndGetAccessTokenAsync(scenario.NonMemberEmail, scenario.NonMemberPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/clubs/{scenario.ClubId}/members?pageIndex=0&pageSize=20", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task SeedEventAsync(int clubId, string title, EventStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Events.Add(new Event
        {
            ClubId = clubId, Title = title, Status = status, Audience = EventAudience.Public,
            StartDateUtc = DateTime.UtcNow.AddDays(1), EndDateUtc = DateTime.UtcNow.AddDays(1).AddHours(2),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
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

    private sealed record Scenario(int ClubId, string PresidentEmail, string PresidentPassword, string NonMemberEmail, string NonMemberPassword);

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
        var advisorEmail = $"vis-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Görünürlük Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        const string presidentPassword = "President!Test123456";
        var presidentEmail = $"vis-president-{suffix}@test.local";
        var presidentUser = new ApplicationUser { UserName = presidentEmail, Email = presidentEmail, EmailConfirmed = true };
        var presidentCreateResult = await userManager.CreateAsync(presidentUser, presidentPassword);
        Assert.True(presidentCreateResult.Succeeded, string.Join("; ", presidentCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(presidentUser, "ClubOfficer");

        var presidentStudent = new Student
        {
            ApplicationUserId = presidentUser.Id, StudentNumber = $"P{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(presidentStudent);
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = presidentStudent.Id, AcademicTermId = term.Id,
            ClubRole = ClubRole.President,
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President),
            JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        const string nonMemberPassword = "Member!Test123456";
        var nonMemberEmail = $"vis-nonmember-{suffix}@test.local";
        var nonMemberUser = new ApplicationUser { UserName = nonMemberEmail, Email = nonMemberEmail, EmailConfirmed = true };
        var nonMemberCreateResult = await userManager.CreateAsync(nonMemberUser, nonMemberPassword);
        Assert.True(nonMemberCreateResult.Succeeded, string.Join("; ", nonMemberCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(nonMemberUser, "Member");

        var nonMemberStudent = new Student
        {
            ApplicationUserId = nonMemberUser.Id, StudentNumber = $"N{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(nonMemberStudent);
        await db.SaveChangesAsync();

        return new Scenario(club.Id, presidentEmail, presidentPassword, nonMemberEmail, nonMemberPassword);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
