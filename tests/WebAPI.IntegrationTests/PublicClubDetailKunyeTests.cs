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

/// <summary>docs/MIMARI.md · K-51/A-82/Y-87: vitrin detayının künye alanları ve görüntülenme sayacı.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class PublicClubDetailKunyeTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PublicClubDetailKunyeTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "K-51: vitrin detayı künye alanlarını taşır")]
    public async Task PublicClubDetail_CarriesProfileStats()
    {
        var clubId = await SeedClubWithMemberAndEventAsync("kunye", foundedYear: 2012);

        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");

        Assert.Equal(2012, body.GetProperty("foundedYear").GetInt32());
        Assert.Equal(1, body.GetProperty("memberCount").GetInt32());
        Assert.Equal(1, body.GetProperty("eventCount").GetInt32());
        Assert.Equal(0, body.GetProperty("viewCount").GetInt32());
    }

    [Fact(DisplayName = "Y-87: kuruluş yılı boşsa alan null döner, CreatedAtUtc'den türetilmez")]
    public async Task PublicClubDetail_LeavesFoundedYearNull_WhenUnknown()
    {
        var clubId = await SeedClubWithMemberAndEventAsync("kunye-null", foundedYear: null);

        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");

        Assert.Equal(JsonValueKind.Null, body.GetProperty("foundedYear").ValueKind);
    }

    [Fact(DisplayName = "A-82: /view ucu kulüp görüntülenmesini artırır, GET artırmaz")]
    public async Task ClubViewEndpoint_IncrementsCounter()
    {
        var clubId = await SeedClubWithMemberAndEventAsync("kunye-view", foundedYear: 2012);

        var before = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");
        Assert.Equal(0, before.GetProperty("viewCount").GetInt32());

        var viewResponse = await _client.PostAsync($"/api/public/clubs/{clubId}/view", null);
        Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);

        var after = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");
        Assert.Equal(1, after.GetProperty("viewCount").GetInt32());
    }

    private async Task<int> SeedClubWithMemberAndEventAsync(string suffix, int? foundedYear)
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

        var advisorEmail = $"kunye-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, "Advisor!Test123456");
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Künye Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow, FoundedYear = foundedYear };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var memberEmail = $"kunye-member-{suffix}@test.local";
        var memberUser = new ApplicationUser { UserName = memberEmail, Email = memberEmail, EmailConfirmed = true };
        var memberCreateResult = await userManager.CreateAsync(memberUser, "Member!Test123456");
        Assert.True(memberCreateResult.Succeeded, string.Join("; ", memberCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(memberUser, "Member");

        var memberStudent = new Student
        {
            ApplicationUserId = memberUser.Id, StudentNumber = $"K{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
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
        await db.SaveChangesAsync();

        return club.Id;
    }
}
