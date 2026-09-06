using System.Net;
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

/// <summary>docs/MIMARI.md · K-46/A-77/Y-82: anonim etkinlik detayı ve görüntülenme sayacı.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class PublicEventDetailTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PublicEventDetailTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Anonim ziyaretçi yayınlanmış ve herkese açık etkinliğin detayını görür (K-46)")]
    public async Task PublicEventDetail_ReturnsPublishedPublicEvent()
    {
        var (clubId, eventId) = await SeedEventAsync("pubdetail-ok", EventStatus.Published, EventAudience.Public);

        var response = await _client.GetAsync($"/api/public/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(eventId, body.GetProperty("id").GetInt32());
        Assert.Equal(clubId, body.GetProperty("clubId").GetInt32());
        Assert.Equal(0, body.GetProperty("participantCount").GetInt32());
    }

    [Fact(DisplayName = "Y-82: taslak etkinliğin detayı anonim uçtan 404 döner")]
    public async Task PublicEventDetail_ReturnsNotFound_ForDraft()
    {
        var (_, eventId) = await SeedEventAsync("pubdetail-draft", EventStatus.Draft, EventAudience.Public);

        var response = await _client.GetAsync($"/api/public/events/{eventId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Y-82: üyelere özel etkinliğin detayı anonim uçtan 404 döner")]
    public async Task PublicEventDetail_ReturnsNotFound_ForMembersOnly()
    {
        var (_, eventId) = await SeedEventAsync("pubdetail-members", EventStatus.Published, EventAudience.ClubMembers);

        var response = await _client.GetAsync($"/api/public/events/{eventId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "A-77: /view ucu görüntülenme sayısını artırır, GET artırmaz")]
    public async Task ViewEndpoint_IncrementsCounter_ButGetDoesNot()
    {
        var (_, eventId) = await SeedEventAsync("pubdetail-view", EventStatus.Published, EventAudience.Public);

        var first = await (await _client.GetAsync($"/api/public/events/{eventId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, first.GetProperty("viewCount").GetInt32());

        var viewResponse = await _client.PostAsync($"/api/public/events/{eventId}/view", null);
        Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);

        var second = await (await _client.GetAsync($"/api/public/events/{eventId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, second.GetProperty("viewCount").GetInt32());
    }

    private async Task<(int ClubId, int EventId)> SeedEventAsync(string suffix, EventStatus status, EventAudience audience)
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
        var advisorEmail = $"pubevt-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Etkinlik Vitrin Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var entity = new Event
        {
            ClubId = club.Id, Title = $"Etkinlik-{suffix}", Status = status, Audience = audience,
            StartDateUtc = DateTime.UtcNow.AddDays(3), EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Events.Add(entity);
        await db.SaveChangesAsync();

        return (club.Id, entity.Id);
    }
}
