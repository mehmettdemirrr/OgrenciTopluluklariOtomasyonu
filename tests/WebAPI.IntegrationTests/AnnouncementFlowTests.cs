using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataAccess;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>docs/PLAN-V2.md §10.4 · A-43: kulüp duyurusu oluştur → akışta gör → soft delete → akıştan kalkar.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class AnnouncementFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AnnouncementFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Danışman duyuru oluşturur → akışta görünür → soft delete sonrası akıştan kalkar")]
    public async Task FullFlow_CreatePublishDelete_WorksEndToEnd()
    {
        var scenario = await SeedScenarioAsync("ann");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.ClubId}/announcements", advisorToken,
            new { Title = "Yeni Dönem Duyurusu", Content = "Kayıtlar başladı.", Visibility = "Members" });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var announcementId = await createResponse.Content.ReadFromJsonAsync<int>();

        var feedResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/announcements", advisorToken);
        Assert.Equal(HttpStatusCode.OK, feedResponse.StatusCode);
        Assert.Contains($"\"id\":{announcementId}", await feedResponse.Content.ReadAsStringAsync());

        var deleteResponse = await SendWithBearerAsync(HttpMethod.Delete, $"/api/announcements/{announcementId}", advisorToken);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var feedAfterDelete = await SendWithBearerAsync(HttpMethod.Get, "/api/announcements", advisorToken);
        Assert.DoesNotContain($"\"id\":{announcementId}", await feedAfterDelete.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Kulüple ilgisi olmayan kullanıcı duyuru oluşturamaz (Forbidden)")]
    public async Task Create_UnrelatedUser_ReturnsForbidden()
    {
        var scenario = await SeedScenarioAsync("annforbidden");

        const string password = "Officer!Test123456";
        var email = "ann-outsider@test.local";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user, password);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, "ClubOfficer");
        }

        var outsiderToken = await LoginAndGetAccessTokenAsync(email, password);
        var response = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.ClubId}/announcements", outsiderToken,
            new { Title = "Deneme", Content = "İçerik", Visibility = "Members" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Admin announcements.global ile kulübe bağlı olmayan sistem duyurusu oluşturabilir")]
    public async Task CreateGlobal_Admin_ReturnsSuccessWithoutClub()
    {
        const string adminPassword = "Admin!Test123456";
        const string adminEmail = "ann-admin@test.local";
        using (var setupScope = _factory.Services.CreateScope())
        {
            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(admin, adminPassword);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        var adminToken = await LoginAndGetAccessTokenAsync(adminEmail, adminPassword);

        var response = await SendWithBearerAsync(
            HttpMethod.Post, "/api/announcements", adminToken,
            new { Title = "Sistem Bakımı", Content = "Cumartesi bakım yapılacaktır.", Visibility = "Public" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var announcementId = await response.Content.ReadFromJsonAsync<int>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var announcement = await db.Announcements.SingleAsync(a => a.Id == announcementId);
        Assert.Null(announcement.ClubId);
    }

    [Fact(DisplayName = "K-47: duyuru düzenleme sayfası için tekil okuma ucu duyuruyu döndürür")]
    public async Task GetById_ReturnsAnnouncement_ForManager()
    {
        var scenario = await SeedScenarioAsync("ann-getbyid");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);
        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.ClubId}/announcements", advisorToken,
            new { Title = "Okunacak Duyuru", Content = "İçerik", Visibility = "Members" });
        var announcementId = await createResponse.Content.ReadFromJsonAsync<int>();

        var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/announcements/{announcementId}", advisorToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Okunacak Duyuru", body.GetProperty("title").GetString());
    }

    [Fact(DisplayName = "Y-35: kulüple ilgisi olmayan kullanıcı tekil duyuru ucundan Forbidden alır")]
    public async Task GetById_ReturnsForbidden_ForUnrelatedUser()
    {
        var scenario = await SeedScenarioAsync("ann-getbyid-forbidden");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);
        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.ClubId}/announcements", advisorToken,
            new { Title = "Gizli", Content = "İçerik", Visibility = "Members" });
        var announcementId = await createResponse.Content.ReadFromJsonAsync<int>();

        const string password = "Officer!Test123456";
        const string email = "ann-getbyid-outsider@test.local";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await userManager.CreateAsync(user, password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, "ClubOfficer");
        }

        var outsiderToken = await LoginAndGetAccessTokenAsync(email, password);
        var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/announcements/{announcementId}", outsiderToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private sealed record Scenario(int ClubId, string AdvisorEmail, string AdvisorPassword);

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

        const string advisorPassword = "Advisor!Test123456";
        var advisorEmail = $"ann-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Duyuru Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        return new Scenario(club.Id, advisorEmail, advisorPassword);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
