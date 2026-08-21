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
/// docs/PLAN-V2.md §10.3 · A-38/Y-53: kontenjanı 1 olan bir etkinliğe paralel iki kayıt denendiğinde
/// tam olarak biri başarılı olmalı — Event.RowVersion'ın gerçekten tüketildiğinin uçtan uca kanıtı.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class EventCapacityConcurrencyTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public EventCapacityConcurrencyTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-38: kontenjanı 1 olan etkinliğe paralel iki kayıt → tam biri başarılı, diğeri 409")]
    public async Task Register_ParallelRequestsOnSingleSlotEvent_ExactlyOneSucceeds()
    {
        var scenario = await SeedScenarioAsync("cap");

        var clientA = _factory.CreateClient();
        var clientB = _factory.CreateClient();
        var tokenA = await LoginAndGetAccessTokenAsync(clientA, scenario.StudentAEmail, scenario.StudentAPassword);
        var tokenB = await LoginAndGetAccessTokenAsync(clientB, scenario.StudentBEmail, scenario.StudentBPassword);

        var taskA = RegisterAsync(clientA, scenario.EventId, tokenA);
        var taskB = RegisterAsync(clientB, scenario.EventId, tokenB);
        var results = await Task.WhenAll(taskA, taskB);

        var successCount = results.Count(r => r == HttpStatusCode.OK);
        var conflictCount = results.Count(r => r == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activeParticipants = await db.EventParticipations.CountAsync(p => p.EventId == scenario.EventId);
        Assert.Equal(1, activeParticipants);
    }

    private static async Task<HttpStatusCode> RegisterAsync(HttpClient client, int eventId, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/participation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private sealed record Scenario(int EventId, string StudentAEmail, string StudentAPassword, string StudentBEmail, string StudentBPassword);

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

        var term = await db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
        if (term is null)
        {
            term = new AcademicTerm { Name = $"2026-Güz-{suffix}", StartDateUtc = DateTime.UtcNow.AddMonths(-1), EndDateUtc = DateTime.UtcNow.AddMonths(3), IsCurrent = true };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();
        }

        var advisorEmail = $"cap-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, "Advisor!Test123456");
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Kontenjan Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var @event = new Event
        {
            ClubId = club.Id,
            Title = "Tek Kişilik Atölye",
            StartDateUtc = DateTime.UtcNow.AddDays(3),
            EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(1),
            Capacity = 1,
            Status = EventStatus.Published,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        const string studentPassword = "Student!Test123456";
        var studentAEmail = $"cap-student-a-{suffix}@test.local";
        var studentBEmail = $"cap-student-b-{suffix}@test.local";

        foreach (var email in new[] { studentAEmail, studentBEmail })
        {
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user, studentPassword);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, "Member");

            var student = new Student { ApplicationUserId = user.Id, StudentNumber = $"C{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026 };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        return new Scenario(@event.Id, studentAEmail, studentPassword, studentBEmail, studentPassword);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
