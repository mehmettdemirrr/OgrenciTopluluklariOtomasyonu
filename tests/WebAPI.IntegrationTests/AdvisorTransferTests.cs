using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md §27 "Çıkış koşulu" (K-33) — Faz 27'nin kabul testi.
///
/// v5.0'a kadar <c>AcademicStaff</c> salt-okunurdu ve kulübün danışmanı yalnızca oluşturma anında
/// belirlenip bir daha değiştirilemiyordu (bildirilen #3). Testin asıl kanıtı **yetki devri**:
/// danışman değişince eskisi o kulüpte artık işlem yapamamalı, yenisi yapabilmeli.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AdvisorTransferTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "adv-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string OldAdvisorEmail = "adv-old@test.local";
    private const string NewAdvisorEmail = "adv-new@test.local";
    private const string AdvisorPassword = "Advisor!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _clubId;
    private int _oldAdvisorStaffId;
    private int _newAdvisorStaffId;
    private int _departmentId;
    private int _freeStaffId;
    private string _suffix = string.Empty;

    public AdvisorTransferTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, IdentitySeedData.AdminRoleName);

        _suffix = Guid.NewGuid().ToString("N")[..8];
        _departmentId = (await db.Departments.FirstAsync()).Id;

        _oldAdvisorStaffId = await EnsureStaffAsync(db, userManager, OldAdvisorEmail, "Doç. Dr.");
        _newAdvisorStaffId = await EnsureStaffAsync(db, userManager, NewAdvisorEmail, "Prof. Dr.");

        // Hiçbir kulübe danışmanlık yapmayan personel — silinebilmeli.
        _freeStaffId = await EnsureStaffAsync(db, userManager, $"adv-free-{_suffix}@test.local", "Dr. Öğr. Üyesi");

        var club = new Club
        {
            Name = $"Devir Kulubu {_suffix}",
            AdvisorId = _oldAdvisorStaffId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        _clubId = club.Id;

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "K-33: danışman değişince ESKİ danışman o kulüpte işlem yapamıyor, YENİ danışman yapabiliyor")]
    public async Task ChangeAdvisor_TransfersAuthority()
    {
        var oldToken = await LoginAsync(OldAdvisorEmail, AdvisorPassword);
        var newToken = await LoginAsync(NewAdvisorEmail, AdvisorPassword);

        // Devirden ÖNCE: eski danışman yapabiliyor, yeni danışman yapamıyor.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/events", oldToken, NewEvent("Devir oncesi"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/events", newToken, NewEvent("Olmamali"))).StatusCode);

        // Devir.
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var update = await SendAsync(HttpMethod.Put, $"/api/clubs/{_clubId}", adminToken, new
        {
            Name = $"Devir Kulubu {_suffix}",
            Description = "Danışman değişti.",
            AdvisorId = _newAdvisorStaffId,
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        // Devirden SONRA: roller tamamen yer değiştirmeli. Ensure* metotları club.AdvisorId'yi
        // okuduğu için ek bir iş gerekmiyor — testin kanıtlamak istediği tam olarak bu.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/events", newToken, NewEvent("Devir sonrasi"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, $"/api/clubs/{_clubId}/events", oldToken, NewEvent("Artik olmamali"))).StatusCode);
    }

    [Fact(DisplayName = "K-33: var olmayan danışmana atama NotFound — kulüp danışmansız kalmaz")]
    public async Task ChangeAdvisor_UnknownAdvisor_ReturnsNotFound()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(HttpMethod.Put, $"/api/clubs/{_clubId}", adminToken, new
        {
            Name = $"Devir Kulubu {_suffix}",
            Description = (string?)null,
            AdvisorId = 999_999,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "K-33: akademik personel oluşturulabiliyor ve kulübe danışman olarak atanabiliyor")]
    public async Task CreateAcademicStaff_ThenAssignAsAdvisor()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        // Önce profilsiz bir kullanıcı (Advisor rolü seçilmediği için Y-67 profil istemez).
        var email = $"adv-fresh-{_suffix}@test.local";
        var createUser = await SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            Email = email,
            Password = "Fresh!Test123456",
            FirstName = "Yeni",
            LastName = "Danisman",
            RoleNames = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.OK, createUser.StatusCode);

        int userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            userId = (await db.Users.SingleAsync(u => u.Email == email)).Id;
        }

        // Sonra o kullanıcıya akademik personel profili — v5.0'a kadar bu uç HİÇ YOKTU.
        var createStaff = await SendAsync(HttpMethod.Post, "/api/academic-staff", adminToken, new
        {
            ApplicationUserId = userId,
            Title = "Dr. Öğr. Üyesi",
            DepartmentId = _departmentId,
        });
        Assert.Equal(HttpStatusCode.OK, createStaff.StatusCode);

        var staffId = await createStaff.Content.ReadFromJsonAsync<int>();

        var assign = await SendAsync(HttpMethod.Put, $"/api/clubs/{_clubId}", adminToken, new
        {
            Name = $"Devir Kulubu {_suffix}",
            Description = (string?)null,
            AdvisorId = staffId,
        });

        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
    }

    [Fact(DisplayName = "K-33: kulübe danışmanlık yapan personel silinemiyor (409), bağsız personel silinebiliyor")]
    public async Task DeleteAcademicStaff_BlockedWhenAdvising()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        var advising = await SendAsync(HttpMethod.Delete, $"/api/academic-staff/{_oldAdvisorStaffId}", adminToken);
        Assert.Equal(HttpStatusCode.Conflict, advising.StatusCode);

        var free = await SendAsync(HttpMethod.Delete, $"/api/academic-staff/{_freeStaffId}", adminToken);
        Assert.Equal(HttpStatusCode.OK, free.StatusCode);
    }

    [Fact(DisplayName = "K-33: akademik personelin unvanı ve bölümü güncellenebiliyor")]
    public async Task UpdateAcademicStaff_ChangesTitle()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(HttpMethod.Put, $"/api/academic-staff/{_newAdvisorStaffId}", adminToken, new
        {
            Title = "Emeritus Prof.",
            DepartmentId = _departmentId,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Emeritus Prof.", (await db.AcademicStaff.SingleAsync(a => a.Id == _newAdvisorStaffId)).Title);
    }

    private object NewEvent(string title) => new
    {
        Title = $"{title} {Guid.NewGuid():N}"[..30],
        Description = "Danışman devri testi",
        Location = "A Salonu",
        StartDateUtc = DateTime.UtcNow.AddDays(14),
        EndDateUtc = DateTime.UtcNow.AddDays(14).AddHours(2),
        Capacity = 20,
    };

    private async Task<int> EnsureStaffAsync(AppDbContext db, UserManager<ApplicationUser> userManager, string email, string title)
    {
        var user = await EnsureUserAsync(userManager, email, AdvisorPassword, IdentitySeedData.AdvisorRoleName);

        var staff = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == user.Id);
        if (staff is null)
        {
            staff = new AcademicStaff { ApplicationUserId = user.Id, Title = title, DepartmentId = _departmentId };
            db.AcademicStaff.Add(staff);
            await db.SaveChangesAsync();
        }

        return staff.Id;
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

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
