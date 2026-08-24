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
/// docs/PLAN-V4.md §22 "Çıkış koşulu" (K-30/A-51) — Faz 22'nin kabul testi.
/// Devirden sonra <c>President</c> rolü korunuyor ve etkinlik oluşturulabiliyor; pasif kulüp ve
/// soft-delete edilmiş üyelik devredilmiyor; ikinci çalıştırma sıfır yeni satır üretiyor.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class TermRolloverTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "tr-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string StudentEmail = "tr-student@test.local";
    private const string StudentPassword = "Student!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    private int _studentId;
    private int _presidentClubId;
    private int _memberClubId;
    private int _inactiveClubId;
    private int _leftClubId;
    private int _oldTermId;
    private int _newTermId;

    public TermRolloverTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Admin");

        // events.write ClubOfficer kimlik rolünden gelir — ClubRole.President tek başına JWT
        // claim'i üretmez (bkz. PLAN-V3 §17 düzeltmesi). İkisi birlikte gerekli.
        var studentUser = await EnsureUserAsync(userManager, StudentEmail, StudentPassword, "ClubOfficer");

        var department = await db.Departments.FirstAsync();

        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == adminUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = adminUser.Id, Title = "Prof. Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == studentUser.Id);
        if (student is null)
        {
            student = new Student
            {
                ApplicationUserId = studentUser.Id,
                StudentNumber = $"TR{Guid.NewGuid():N}"[..12],
                DepartmentId = department.Id,
                EnrollmentYear = 2026,
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        _studentId = student.Id;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        _presidentClubId = await AddClubAsync(db, $"Devir Baskan {suffix}", advisor.Id, isActive: true);
        _memberClubId = await AddClubAsync(db, $"Devir Uye {suffix}", advisor.Id, isActive: true);
        _inactiveClubId = await AddClubAsync(db, $"Devir Pasif {suffix}", advisor.Id, isActive: false);
        _leftClubId = await AddClubAsync(db, $"Devir Ayrilan {suffix}", advisor.Id, isActive: true);

        // Devrin kaynağı: güncel dönem. Testler aynı süreçte sıralı çalıştığı için mevcut
        // güncel dönemi bozmamak adına kendi dönemlerimizi kuruyoruz.
        var previous = await db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
        if (previous is not null)
        {
            previous.IsCurrent = false;
            await db.SaveChangesAsync();
        }

        var oldTerm = new AcademicTerm
        {
            Name = $"Devir Kaynak {suffix}",
            StartDateUtc = DateTime.UtcNow.AddMonths(-6),
            EndDateUtc = DateTime.UtcNow.AddMonths(-1),
            IsCurrent = true,
        };
        var newTerm = new AcademicTerm
        {
            Name = $"Devir Hedef {suffix}",
            StartDateUtc = DateTime.UtcNow,
            EndDateUtc = DateTime.UtcNow.AddMonths(5),
            IsCurrent = false,
        };
        db.AcademicTerms.AddRange(oldTerm, newTerm);
        await db.SaveChangesAsync();

        _oldTermId = oldTerm.Id;
        _newTermId = newTerm.Id;

        db.ClubMemberships.AddRange(
            new ClubMembership { ClubId = _presidentClubId, StudentId = _studentId, AcademicTermId = _oldTermId, ClubRole = ClubRole.President, JoinedAtUtc = DateTime.UtcNow.AddMonths(-5) },
            new ClubMembership { ClubId = _memberClubId, StudentId = _studentId, AcademicTermId = _oldTermId, ClubRole = ClubRole.Member, JoinedAtUtc = DateTime.UtcNow.AddMonths(-5) },
            new ClubMembership { ClubId = _inactiveClubId, StudentId = _studentId, AcademicTermId = _oldTermId, ClubRole = ClubRole.Officer, JoinedAtUtc = DateTime.UtcNow.AddMonths(-5) },
            new ClubMembership
            {
                ClubId = _leftClubId,
                StudentId = _studentId,
                AcademicTermId = _oldTermId,
                ClubRole = ClubRole.Member,
                JoinedAtUtc = DateTime.UtcNow.AddMonths(-5),
                IsDeleted = true,
                DeletedAtUtc = DateTime.UtcNow.AddMonths(-2),
            });

        await db.SaveChangesAsync();

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-51: devir rolleri korur, pasif kulübü ve ayrılmış üyeliği atlar, ikinci çalıştırma sıfır satır üretir")]
    public async Task SetCurrent_CarriesMembershipsWithRoles_AndIsIdempotent()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(HttpMethod.Put, $"/api/academic-terms/{_newTermId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<ClubMembership> carried;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            carried = await db.ClubMemberships.Where(m => m.AcademicTermId == _newTermId && m.StudentId == _studentId).ToListAsync();

            // Aktif iki kulüp devredildi; pasif kulüp ve soft-delete edilmiş üyelik devredilmedi.
            Assert.Equal(2, carried.Count);
            Assert.Equal(ClubRole.President, carried.Single(m => m.ClubId == _presidentClubId).ClubRole);
            Assert.Equal(ClubRole.Member, carried.Single(m => m.ClubId == _memberClubId).ClubRole);
            Assert.DoesNotContain(carried, m => m.ClubId == _inactiveClubId);
            Assert.DoesNotContain(carried, m => m.ClubId == _leftClubId);

            // Kaynak dönemin satırları olduğu gibi duruyor — devir kopyalar, taşımaz.
            Assert.Equal(3, await db.ClubMemberships.CountAsync(m => m.AcademicTermId == _oldTermId && m.StudentId == _studentId));
        }

        // Idempotentlik: dönem zaten güncel → AlreadyCurrent, yeni satır yok.
        var second = await SendAsync(HttpMethod.Put, $"/api/academic-terms/{_newTermId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var afterSecond = await db.ClubMemberships.CountAsync(m => m.AcademicTermId == _newTermId && m.StudentId == _studentId);
            Assert.Equal(carried.Count, afterSecond);
        }
    }

    [Fact(DisplayName = "A-51'in varlık sebebi: devredilen President yeni dönemde etkinlik oluşturabiliyor (yetki kaybı yok)")]
    public async Task CarriedPresident_CanStillCreateEvent()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        await SendAsync(HttpMethod.Put, $"/api/academic-terms/{_newTermId}/current", adminToken);

        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        var create = await SendAsync(
            HttpMethod.Post,
            $"/api/clubs/{_presidentClubId}/events",
            studentToken,
            new
            {
                Title = "Devir Sonrasi Etkinlik",
                Description = "Baskanlik yeni doneme tasindi.",
                Location = "A Salonu",
                StartDateUtc = DateTime.UtcNow.AddDays(10),
                EndDateUtc = DateTime.UtcNow.AddDays(10).AddHours(2),
                Capacity = 30,
            });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        // Yalnızca Member olarak devredilen kulüpte hâlâ yetkisi yok (Y-23 kapsam kuralı korunuyor).
        var forbidden = await SendAsync(
            HttpMethod.Post,
            $"/api/clubs/{_memberClubId}/events",
            studentToken,
            new
            {
                Title = "Yetkisiz Deneme",
                Description = "Member rolüyle olmamalı.",
                Location = "B Salonu",
                StartDateUtc = DateTime.UtcNow.AddDays(10),
                EndDateUtc = DateTime.UtcNow.AddDays(10).AddHours(2),
                Capacity = 30,
            });

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact(DisplayName = "A-51 idempotentlik: hedef dönemde zaten var olan (kulüp, öğrenci) çifti kopyalanmaz — Y-18 index'i ihlal edilmez")]
    public async Task Rollover_SkipsMembershipsThatAlreadyExistInTargetTerm()
    {
        // Devir çalışmadan ÖNCE hedef döneme elle bir üyelik konur; ikinci kopya unique index'i
        // ihlal ederdi. SetCurrentAsync'in AlreadyCurrent kısa devresi burada devrede değil —
        // kopyalama döngüsünün kendi atlama kuralı sınanıyor.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ClubMemberships.Add(new ClubMembership
            {
                ClubId = _memberClubId,
                StudentId = _studentId,
                AcademicTermId = _newTermId,
                ClubRole = ClubRole.Officer,
                JoinedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var response = await SendAsync(HttpMethod.Put, $"/api/academic-terms/{_newTermId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.ClubMemberships.Where(m => m.AcademicTermId == _newTermId && m.StudentId == _studentId).ToListAsync();

            Assert.Equal(2, rows.Count);

            // Elle konulan satır ezilmedi: rolü Officer olarak kaldı (kaynakta Member'dı).
            Assert.Equal(ClubRole.Officer, rows.Single(m => m.ClubId == _memberClubId).ClubRole);
            Assert.Equal(ClubRole.President, rows.Single(m => m.ClubId == _presidentClubId).ClubRole);
        }
    }

    [Fact(DisplayName = "A-39 × A-51: hedef dönemde kulübün başkanı zaten varsa devredilen başkan Officer'a düşer (index ihlali yok, yetki kaybı yok)")]
    public async Task Rollover_DemotesPresidentWhenTargetTermAlreadyHasOne()
    {
        int otherStudentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var otherUser = await EnsureUserAsync(userManager, "tr-other@test.local", "Other!Test123456", "Member");
            var other = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == otherUser.Id);
            if (other is null)
            {
                var department = await db.Departments.FirstAsync();
                other = new Student
                {
                    ApplicationUserId = otherUser.Id,
                    StudentNumber = $"TO{Guid.NewGuid():N}"[..12],
                    DepartmentId = department.Id,
                    EnrollmentYear = 2026,
                };
                db.Students.Add(other);
                await db.SaveChangesAsync();
            }

            otherStudentId = other.Id;

            // Hedef döneme BAŞKA bir öğrenci başkan olarak atanmış.
            db.ClubMemberships.Add(new ClubMembership
            {
                ClubId = _presidentClubId,
                StudentId = otherStudentId,
                AcademicTermId = _newTermId,
                ClubRole = ClubRole.President,
                JoinedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var response = await SendAsync(HttpMethod.Put, $"/api/academic-terms/{_newTermId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var carriedPresident = await db.ClubMemberships
                .SingleAsync(m => m.AcademicTermId == _newTermId && m.StudentId == _studentId && m.ClubId == _presidentClubId);

            // Elle atanan başkanın sözü geçer; devredilen başkan Officer'a düşer.
            Assert.Equal(ClubRole.Officer, carriedPresident.ClubRole);
            Assert.Equal(
                ClubRole.President,
                (await db.ClubMemberships.SingleAsync(m => m.AcademicTermId == _newTermId && m.StudentId == otherStudentId)).ClubRole);
        }

        // Y-23 açısından kayıp yok: Officer da kulübün etkinliğini oluşturabilir.
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);
        var create = await SendAsync(
            HttpMethod.Post,
            $"/api/clubs/{_presidentClubId}/events",
            studentToken,
            new
            {
                Title = "Officer'a dusen baskan",
                Description = "Yetki korunuyor.",
                Location = "C Salonu",
                StartDateUtc = DateTime.UtcNow.AddDays(12),
                EndDateUtc = DateTime.UtcNow.AddDays(12).AddHours(2),
                Capacity = 20,
            });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
    }

    [Fact(DisplayName = "§22.3: /api/clubs/mine yalnızca güncel dönemi döner ve dönem adını taşır")]
    public async Task GetMine_ReturnsOnlyCurrentTerm()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        await SendAsync(HttpMethod.Put, $"/api/academic-terms/{_newTermId}/current", adminToken);

        var studentToken = await LoginAsync(StudentEmail, StudentPassword);
        var response = await SendAsync(HttpMethod.Get, "/api/clubs/mine", studentToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = (await response.Content.ReadFromJsonAsync<List<MyClubRow>>())!;

        // Eski dönemde 3, yeni dönemde 2 üyelik var; dönem filtresi olmasaydı 5 satır dönerdi.
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Contains("Devir Hedef", item.AcademicTermName, StringComparison.Ordinal));
        Assert.Contains(items, item => item.ClubId == _presidentClubId && item.ClubRole == "President");
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

    private static async Task<int> AddClubAsync(AppDbContext db, string name, int advisorId, bool isActive)
    {
        var club = new Club { Name = name, AdvisorId = advisorId, IsActive = isActive, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        return club.Id;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
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

    private sealed class MyClubRow
    {
        public int ClubId { get; init; }

        public string ClubName { get; init; } = string.Empty;

        public string ClubRole { get; init; } = string.Empty;

        public string AcademicTermName { get; init; } = string.Empty;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
