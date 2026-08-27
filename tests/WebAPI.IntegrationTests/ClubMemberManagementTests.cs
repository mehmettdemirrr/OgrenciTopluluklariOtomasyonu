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
/// docs/PLAN-V2.md §9 — Faz 9'un "bitti sayılır" kanıtı: bir öğrenciye President verilip
/// EventManager.EnsureClubWriteAccessAsync'in Officer/President dalı ilk kez uçtan uca çalışıyor.
/// A-39 (başkan tekilliği) ve Y-23 (kaynak kapsamı) kuralları da burada uçtan uca doğrulanır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class ClubMemberManagementTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ClubMemberManagementTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Danışman öğrenciyi President yapar → öğrenci artık etkinlik oluşturabilir (ölü kodun canlandığının kanıtı)")]
    public async Task AdvisorPromotesStudentToPresident_StudentCanThenCreateEvent()
    {
        var scenario = await SeedScenarioAsync("promote");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        // events.write claim'i baştan beri var (ClubOfficer kimlik rolü) ama BU kulüpte ClubRole=Member
        // olduğu için EnsureClubWriteAccessAsync henüz izin vermiyor — Y-23'ün canlı kanıtı.
        var officerToken = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);
        var beforeResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.Club.Id}/events", officerToken,
            new { Title = "Erken Deneme", StartDateUtc = DateTime.UtcNow.AddDays(5), EndDateUtc = DateTime.UtcNow.AddDays(5).AddHours(2) });
        Assert.Equal(HttpStatusCode.Forbidden, beforeResponse.StatusCode);

        var promoteResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/clubs/{scenario.Club.Id}/members/{scenario.MembershipId}/role", advisorToken,
            new { ClubRole = "President" });
        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);

        // Aynı kullanıcı yeniden giriş yapar (izinler her refresh/login'de yeniden çözülür — K-01).
        var presidentToken = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);
        var afterResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.Club.Id}/events", presidentToken,
            new { Title = "Başkanlık Sonrası Etkinlik", StartDateUtc = DateTime.UtcNow.AddDays(5), EndDateUtc = DateTime.UtcNow.AddDays(5).AddHours(2) });
        Assert.Equal(HttpStatusCode.OK, afterResponse.StatusCode);
    }

    [Fact(DisplayName = "A-39: bir kulüpte aynı dönemde ikinci President atanamaz (409)")]
    public async Task SecondPresidentAssignment_ReturnsConflict()
    {
        var scenario = await SeedScenarioAsync("uniq");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var firstPromote = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/clubs/{scenario.Club.Id}/members/{scenario.MembershipId}/role", advisorToken,
            new { ClubRole = "President" });
        Assert.Equal(HttpStatusCode.OK, firstPromote.StatusCode);

        var secondMembershipId = await AddPlainMemberAsync(scenario.Club.Id, scenario.TermId, "uniq2");
        var secondPromote = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/clubs/{scenario.Club.Id}/members/{secondMembershipId}/role", advisorToken,
            new { ClubRole = "President" });

        Assert.Equal(HttpStatusCode.Conflict, secondPromote.StatusCode);
    }

    [Fact(DisplayName = "memberships.write taşıyan ama danışman/başkan olmayan biri rol değiştiremez")]
    public async Task SetRole_NotAdvisorOrPresident_ReturnsForbidden()
    {
        var scenario = await SeedScenarioAsync("forbidden");
        // ClubOfficer rolü memberships.write taşır ama BU kulübün danışmanı/başkanı değildir.
        var outsiderOfficerToken = await SeedAndLoginClubOfficerAsync("outsider");

        var response = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/clubs/{scenario.Club.Id}/members/{scenario.MembershipId}/role", outsiderOfficerToken,
            new { ClubRole = "Officer" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "GET /clubs/{id}/members: bu kulüpte henüz Officer/President olmayan biri listeyi göremez")]
    public async Task GetMembers_NotOfficerOrPresidentInThisClub_ReturnsForbidden()
    {
        var scenario = await SeedScenarioAsync("viewforbidden");
        // events.write/memberships.read claim'i var ama BU kulüpte ClubRole hâlâ Member — Y-23.
        var officerToken = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/clubs/{scenario.Club.Id}/members", officerToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "O-20: danışman kendi kulübüne rol tanımı ekler; tanım o kulübün listesinde görünür, başka kulüpte görünmez")]
    public async Task RoleDefinitions_ScopedToClub()
    {
        var scenario = await SeedScenarioAsync("roledefs");
        var otherScenario = await SeedScenarioAsync("roledefs-other");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var name = $"Sosyal Medya {Guid.NewGuid():N}"[..24];
        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.Club.Id}/role-definitions", advisorToken,
            new { Name = name, ClubRole = "Officer", DisplayOrder = 6 });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listResponse = await SendWithBearerAsync(HttpMethod.Get, $"/api/clubs/{scenario.Club.Id}/role-definitions", advisorToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(name, await listResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        // O-20: kapsam kulüptür — diğer kulübün danışmanı bu unvanı GÖREMEZ.
        var otherAdvisorToken = await LoginAndGetAccessTokenAsync(otherScenario.AdvisorEmail, otherScenario.AdvisorPassword);
        var otherListResponse = await SendWithBearerAsync(
            HttpMethod.Get, $"/api/clubs/{otherScenario.Club.Id}/role-definitions", otherAdvisorToken);
        Assert.Equal(HttpStatusCode.OK, otherListResponse.StatusCode);
        Assert.DoesNotContain(name, await otherListResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Y-23: bu kulüpte başkan olmayan biri rol tanımı oluşturamaz — 403")]
    public async Task CreateRoleDefinition_NotPresidentInThisClub_ReturnsForbidden()
    {
        var scenario = await SeedScenarioAsync("roledef-forbidden");
        // memberships.write claim'i var ama BU kulüpte ClubRole hâlâ Member — Y-23.
        var officerToken = await LoginAndGetAccessTokenAsync(scenario.OfficerEmail, scenario.OfficerPassword);

        var response = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.Club.Id}/role-definitions", officerToken,
            new { Name = "Yetkisiz Unvan", ClubRole = "Officer", DisplayOrder = 9 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "A-61: kullanımdaki unvan silinemez — 409; unvan atanınca yetki seviyesi tanımdan gelir")]
    public async Task DeleteRoleDefinition_InUse_ReturnsConflict()
    {
        var scenario = await SeedScenarioAsync("roledef-inuse");
        var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);

        var name = $"Kullanimda {Guid.NewGuid():N}"[..22];
        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{scenario.Club.Id}/role-definitions", advisorToken,
            new { Name = name, ClubRole = "Officer", DisplayOrder = 7 });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        int definitionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            definitionId = (await db.ClubRoleDefinitions.SingleAsync(d => d.ClubId == scenario.Club.Id && d.Name == name)).Id;
        }

        // A-61/Y-22: istemci ClubRole GÖNDERMİYOR — sunucu tanımdan okumak zorunda.
        var assignResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/clubs/{scenario.Club.Id}/members/{scenario.MembershipId}/role", advisorToken,
            new { ClubRoleDefinitionId = definitionId });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.ClubMemberships.AsNoTracking().SingleAsync(m => m.Id == scenario.MembershipId);
            Assert.Equal(ClubRole.Officer, membership.ClubRole);
            Assert.Equal(definitionId, membership.ClubRoleDefinitionId);
        }

        var deleteResponse = await SendWithBearerAsync(
            HttpMethod.Delete, $"/api/clubs/{scenario.Club.Id}/role-definitions/{definitionId}", advisorToken);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    private async Task<int> AddPlainMemberAsync(int clubId, int termId, string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var department = await db.Departments.FirstAsync();

        var email = $"clb-member-{suffix}@test.local";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, "Member!Test123456");
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Member");

        var student = new Student { ApplicationUserId = user.Id, StudentNumber = $"M{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026 };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var membership = new ClubMembership { ClubId = clubId, StudentId = student.Id, AcademicTermId = termId, ClubRole = ClubRole.Member, JoinedAtUtc = DateTime.UtcNow };
        db.ClubMemberships.Add(membership);
        await db.SaveChangesAsync();

        return membership.Id;
    }

    private async Task<string> SeedAndLoginClubOfficerAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string password = "Officer!Test123456";
        var email = $"clb-outside-officer-{suffix}@test.local";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "ClubOfficer");

        return await LoginAndGetAccessTokenAsync(email, password);
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
        Club Club, int TermId, int MembershipId, string AdvisorEmail, string AdvisorPassword, string OfficerEmail, string OfficerPassword);

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
        var advisorEmail = $"clb-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail, EmailConfirmed = true };
        var advisorCreateResult = await userManager.CreateAsync(advisorUser, advisorPassword);
        Assert.True(advisorCreateResult.Succeeded, string.Join("; ", advisorCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Yönetim Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        // ÖNEMLİ: "ClubOfficer" KİMLİK rolü seçilir (events.write claim'ini taşıyan tek öğrenci rolü) —
        // ama ClubMembership.ClubRole = Member ile başlar. Bu, Y-23'ün tam kanıtı: izin claim'i
        // (events.write) hep vardı, değişen şey yalnızca BU kulüpteki ClubRole'ün President olması.
        // "Member" kimlik rolü events.write taşımadığı için o rolle bu senaryo kurulamaz.
        const string officerPassword = "Officer!Test123456";
        var officerEmail = $"clb-officer-{suffix}@test.local";
        var officerUser = new ApplicationUser { UserName = officerEmail, Email = officerEmail, EmailConfirmed = true };
        var officerCreateResult = await userManager.CreateAsync(officerUser, officerPassword);
        Assert.True(officerCreateResult.Succeeded, string.Join("; ", officerCreateResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(officerUser, "ClubOfficer");

        var student = new Student
        {
            ApplicationUserId = officerUser.Id, StudentNumber = $"P{Guid.NewGuid():N}"[..12], DepartmentId = department.Id, EnrollmentYear = 2026,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var membership = new ClubMembership
        {
            ClubId = club.Id, StudentId = student.Id, AcademicTermId = term.Id, ClubRole = ClubRole.Member, JoinedAtUtc = DateTime.UtcNow,
        };
        db.ClubMemberships.Add(membership);
        await db.SaveChangesAsync();

        return new Scenario(club, term.Id, membership.Id, advisorEmail, advisorPassword, officerEmail, officerPassword);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
