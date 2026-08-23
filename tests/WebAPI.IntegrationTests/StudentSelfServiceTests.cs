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
/// docs/PLAN-V4.md §19 "Çıkış koşulu" (A-48): başvuru takibi/geri çekme, kulüpten ayrılma,
/// profil düzenleme. Y-22: hedef kayıt hep `ICurrentUser`'dan çözülür, istemciden değil.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class StudentSelfServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string StudentEmail = "ss-student@test.local";
    private const string StudentPassword = "Student!Test123456";
    private const string OtherStudentEmail = "ss-other@test.local";
    private const string OtherStudentPassword = "OtherStudent!Test123456";
    private const string AdvisorEmail = "ss-advisor@test.local";
    private const string AdvisorPassword = "Advisor!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _clubId;
    private int _termId;
    private int _studentId;
    private int _otherDepartmentId;

    public StudentSelfServiceTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var studentUser = await EnsureUserAsync(userManager, StudentEmail, StudentPassword, "Member");
        await EnsureUserAsync(userManager, OtherStudentEmail, OtherStudentPassword, "Member");
        var advisorUser = await EnsureUserAsync(userManager, AdvisorEmail, AdvisorPassword, "Advisor");

        var department = await db.Departments.FirstAsync();
        _termId = (await db.AcademicTerms.FirstAsync(t => t.IsCurrent)).Id;

        // Profil güncelleme testi için ikinci bir bölüm gerekir.
        var secondDepartment = await db.Departments.FirstOrDefaultAsync(d => d.Id != department.Id);
        if (secondDepartment is null)
        {
            secondDepartment = new Department { Name = $"SS Bölüm {Guid.NewGuid():N}"[..20], FacultyId = department.FacultyId };
            db.Departments.Add(secondDepartment);
            await db.SaveChangesAsync();
        }

        _otherDepartmentId = secondDepartment.Id;

        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == advisorUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        foreach (var email in new[] { StudentEmail, OtherStudentEmail })
        {
            var user = await userManager.FindByEmailAsync(email);
            if (!await db.Students.AnyAsync(s => s.ApplicationUserId == user!.Id))
            {
                db.Students.Add(new Student
                {
                    ApplicationUserId = user!.Id,
                    StudentNumber = $"SS{Guid.NewGuid():N}"[..12],
                    DepartmentId = department.Id,
                    EnrollmentYear = 2026,
                });
                await db.SaveChangesAsync();
            }
        }

        _studentId = (await db.Students.FirstAsync(s => s.ApplicationUserId == studentUser.Id)).Id;

        var club = new Club
        {
            Name = $"Self Servis Kulübü {Guid.NewGuid():N}"[..30],
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

    [Fact(DisplayName = "Başvuru geri çekme: /mine'da görünür → geri çekilir → aynı kulübe YENİDEN başvurulabilir")]
    public async Task Withdraw_RemovesApplication_AndAllowsReapplying()
    {
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        var applyResponse = await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/membership-applications", studentToken);
        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        // A-48: öğrenci artık kendi başvurusunu görebiliyor.
        var mineResponse = await SendAsync(HttpMethod.Get, "/api/membership-applications/mine", studentToken);
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        var mineBody = await mineResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Pending\"", mineBody);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.MembershipApplications.SingleAsync(a => a.ClubId == _clubId && a.StudentId == _studentId)).Id;
        }

        var withdrawResponse = await SendAsync(HttpMethod.Delete, $"/api/membership-applications/{applicationId}", studentToken);
        Assert.Equal(HttpStatusCode.OK, withdrawResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Y-16: fiziksel silme yok — query filter yüzünden normal sorguda görünmez.
            Assert.False(await db.MembershipApplications.AnyAsync(a => a.Id == applicationId));
            Assert.True(await db.MembershipApplications.IgnoreQueryFilters().AnyAsync(a => a.Id == applicationId && a.IsDeleted));
        }

        // Fazın asıl kanıtı: filtreli unique index (Status = Pending AND IsDeleted = 0) satırı
        // artık saymadığı için yeniden başvuru serbest.
        var reapplyResponse = await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/membership-applications", studentToken);
        Assert.Equal(HttpStatusCode.OK, reapplyResponse.StatusCode);
    }

    [Fact(DisplayName = "Y-22: başka öğrencinin başvurusu geri çekilemez (403)")]
    public async Task Withdraw_OtherStudentsApplication_ReturnsForbidden()
    {
        var otherToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/membership-applications", studentToken);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.MembershipApplications
                .Where(a => a.ClubId == _clubId && a.StudentId == _studentId)
                .OrderByDescending(a => a.Id)
                .FirstAsync()).Id;
        }

        var response = await SendAsync(HttpMethod.Delete, $"/api/membership-applications/{applicationId}", otherToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Kulüpten ayrılma: üyelik soft delete edilir; son başkan ayrılamaz (409)")]
    public async Task Leave_RemovesMembership_ButLastPresidentCannotLeave()
    {
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        // 1. Sıradan üye olarak ayrılabiliyor.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ClubMemberships.Add(new ClubMembership
            {
                ClubId = _clubId,
                StudentId = _studentId,
                AcademicTermId = _termId,
                ClubRole = ClubRole.Member,
                JoinedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var leaveResponse = await SendAsync(HttpMethod.Delete, $"/api/clubs/{_clubId}/membership", studentToken);
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.ClubMemberships.AnyAsync(m => m.ClubId == _clubId && m.StudentId == _studentId));
        }

        // 2. Tek başkan olarak ayrılamıyor — §19.2, kulüp yönetilemez kalmasın diye.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ClubMemberships.Add(new ClubMembership
            {
                ClubId = _clubId,
                StudentId = _studentId,
                AcademicTermId = _termId,
                ClubRole = ClubRole.President,
                JoinedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var presidentLeaveResponse = await SendAsync(HttpMethod.Delete, $"/api/clubs/{_clubId}/membership", studentToken);
        Assert.Equal(HttpStatusCode.Conflict, presidentLeaveResponse.StatusCode);
    }

    [Fact(DisplayName = "Profil: /me öğrenci alanlarını döner; PUT bölümü değiştirir ama öğrenci numarasına DOKUNMAZ")]
    public async Task UpdateMe_ChangesDepartment_ButNeverStudentNumber()
    {
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        var meResponse = await SendAsync(HttpMethod.Get, "/api/me", studentToken);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meBody = await meResponse.Content.ReadAsStringAsync();

        // Faz 19.3: bu alanlar önceden hiç dönmüyordu.
        Assert.Contains("\"studentNumber\"", meBody);
        Assert.Contains("\"departmentName\"", meBody);
        Assert.Contains("\"facultyName\"", meBody);

        string originalStudentNumber;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            originalStudentNumber = (await db.Students.SingleAsync(s => s.Id == _studentId)).StudentNumber;
        }

        var updateResponse = await SendAsync(
            HttpMethod.Put, "/api/me", studentToken, new { DepartmentId = _otherDepartmentId, EnrollmentYear = 2025 });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var student = await db.Students.SingleAsync(s => s.Id == _studentId);

            Assert.Equal(_otherDepartmentId, student.DepartmentId);
            Assert.Equal(2025, student.EnrollmentYear);

            // §19.2: öğrenci numarası kimliğin parçası — DTO'da alanı bile yok.
            Assert.Equal(originalStudentNumber, student.StudentNumber);
        }
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
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

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
