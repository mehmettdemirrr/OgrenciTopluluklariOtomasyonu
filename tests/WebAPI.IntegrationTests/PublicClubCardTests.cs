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

/// <summary>docs/MIMARI.md · K-50/A-81: vitrin kartı sayıları anonim uçtan gerçekten gelir.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class PublicClubCardTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PublicClubCardTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-81: vitrin kartı güncel dönem üye sayısını ve yalnızca yayınlanmış herkese açık etkinlikleri sayar")]
    public async Task PublicClubs_CarryCurrentTermMemberCountAndVisibleEventCount()
    {
        var clubId = await SeedClubWithOneMemberAndTwoEventsAsync("clubcard");

        var response = await _client.GetAsync("/api/public/clubs?pageIndex=0&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        var card = page.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == clubId);

        Assert.Equal(1, card.GetProperty("memberCount").GetInt32());
        Assert.Equal(1, card.GetProperty("eventCount").GetInt32());   // taslak sayılmaz
        Assert.False(card.TryGetProperty("members", out _));           // Y-58: liste sızmaz
    }

    private async Task<int> SeedClubWithOneMemberAndTwoEventsAsync(string suffix)
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

        // IX_AcademicTerms_IsCurrent: bu koleksiyondaki tüm fact'ler AYNI factory/veritabanını paylaşır.
        var term = await db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
        if (term is null)
        {
            term = new AcademicTerm { Name = $"2026-Güz-{suffix}", StartDateUtc = DateTime.UtcNow.AddMonths(-1), EndDateUtc = DateTime.UtcNow.AddMonths(3), IsCurrent = true };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();
        }

        var advisorEmail = $"card-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, "Advisor!Test123456");
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Kart Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var memberEmail = $"card-member-{suffix}@test.local";
        var memberUser = new ApplicationUser { UserName = memberEmail, Email = memberEmail, EmailConfirmed = true };
        var memberCreateResult = await userManager.CreateAsync(memberUser, "Member!Test123456");
        Assert.True(memberCreateResult.Succeeded, string.Join("; ", memberCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(memberUser, "Member");

        var memberStudent = new Student
        {
            ApplicationUserId = memberUser.Id, StudentNumber = $"C{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(memberStudent);
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = memberStudent.Id, AcademicTermId = term.Id,
            ClubRole = ClubRole.President,
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President),
            JoinedAtUtc = DateTime.UtcNow,
        });

        db.Events.Add(new Event
        {
            ClubId = club.Id, Title = $"Yayında-{suffix}", Status = EventStatus.Published, Audience = EventAudience.Public,
            StartDateUtc = DateTime.UtcNow.AddDays(1), EndDateUtc = DateTime.UtcNow.AddDays(1).AddHours(2), CreatedAtUtc = DateTime.UtcNow,
        });
        db.Events.Add(new Event
        {
            ClubId = club.Id, Title = $"Taslak-{suffix}", Status = EventStatus.Draft, Audience = EventAudience.Public,
            StartDateUtc = DateTime.UtcNow.AddDays(1), EndDateUtc = DateTime.UtcNow.AddDays(1).AddHours(2), CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return club.Id;
    }
}
