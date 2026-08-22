using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 7 §0.6: fakülte/bölüm "seed ucu" — idempotent create-if-missing.
/// Y-45'in ikinci bağımsız kanıtı: [CacheAspect]/[CacheRemoveAspect] çifti burada da
/// (RolePermissionCatalog dışında) çalışıyor.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class ReferenceDataEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "rds-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ReferenceDataEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(admin, AdminPassword);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Aynı fakülte adı iki kez POST edilirse ikisi de 200 döner ve aynı Id'yi taşır")]
    public async Task CreateFaculty_PostedTwice_ReturnsSameIdBothTimes()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"rds-faculty-{suffix}";
        var adminToken = await LoginAndGetAccessTokenAsync();

        var first = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = name });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<FacultyDto>();

        var second = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = name });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<FacultyDto>();

        Assert.Equal(firstBody!.Id, secondBody!.Id);
    }

    [Fact(DisplayName = "POST sonrası fakülte GET listesinde görünür (cache çiftinin ikinci kanıtı)")]
    public async Task CreateFaculty_ThenGet_AppearsInList()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"rds-visible-{suffix}";
        var adminToken = await LoginAndGetAccessTokenAsync();

        var createResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = name });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var getResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/faculties?pageIndex=0&pageSize=200", adminToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Contains(name, await getResponse.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Olmayan fakülteye bölüm eklenmeye çalışılırsa 404 döner")]
    public async Task CreateDepartment_FacultyNotFound_ReturnsNotFound()
    {
        var adminToken = await LoginAndGetAccessTokenAsync();

        var response = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties/999999/departments", adminToken, new { Name = "Hayali Bölüm" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Faz 13: PUT ile fakülte adı güncellenir ve liste yeni adı yansıtır")]
    public async Task UpdateFaculty_ValidRename_ReflectedInList()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();

        var createResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = $"rds-upd-old-{suffix}" });
        var created = await createResponse.Content.ReadFromJsonAsync<FacultyDto>();

        var newName = $"rds-upd-new-{suffix}";
        var updateResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/faculties/{created!.Id}", adminToken, new { Name = newName });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/faculties?pageIndex=0&pageSize=200", adminToken);
        Assert.Contains(newName, await getResponse.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Faz 13: PUT ile bölüm adı güncellenir")]
    public async Task UpdateDepartment_ValidRename_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();

        var facultyResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = $"rds-dept-fac-{suffix}" });
        var faculty = await facultyResponse.Content.ReadFromJsonAsync<FacultyDto>();

        var deptResponse = await SendWithBearerAsync(HttpMethod.Post, $"/api/faculties/{faculty!.Id}/departments", adminToken, new { Name = $"rds-dept-old-{suffix}" });
        var department = await deptResponse.Content.ReadFromJsonAsync<FacultyDto>();

        var newName = $"rds-dept-new-{suffix}";
        var updateResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/faculties/{faculty.Id}/departments/{department!.Id}", adminToken, new { Name = newName });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await SendWithBearerAsync(HttpMethod.Get, $"/api/faculties/{faculty.Id}/departments?pageIndex=0&pageSize=200", adminToken);
        Assert.Contains(newName, await getResponse.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Faz 13 (A-12): kullanımda olmayan bölüm hard delete edilir (204/200)")]
    public async Task DeleteDepartment_NotInUse_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();

        var facultyResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = $"rds-del-fac-{suffix}" });
        var faculty = await facultyResponse.Content.ReadFromJsonAsync<FacultyDto>();
        var deptResponse = await SendWithBearerAsync(HttpMethod.Post, $"/api/faculties/{faculty!.Id}/departments", adminToken, new { Name = $"rds-del-dept-{suffix}" });
        var department = await deptResponse.Content.ReadFromJsonAsync<FacultyDto>();

        var deleteResponse = await SendWithBearerAsync(HttpMethod.Delete, $"/api/faculties/{faculty.Id}/departments/{department!.Id}", adminToken);
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var getResponse = await SendWithBearerAsync(HttpMethod.Get, $"/api/faculties/{faculty.Id}/departments?pageIndex=0&pageSize=200", adminToken);
        Assert.DoesNotContain($"rds-del-dept-{suffix}", await getResponse.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Faz 13 (A-12): kullanımda olan bölüm (kayıtlı öğrenci var) 409 ile silinemez")]
    public async Task DeleteDepartment_InUse_ReturnsConflict()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();

        var facultyResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/faculties", adminToken, new { Name = $"rds-inuse-fac-{suffix}" });
        var faculty = await facultyResponse.Content.ReadFromJsonAsync<FacultyDto>();
        var deptResponse = await SendWithBearerAsync(HttpMethod.Post, $"/api/faculties/{faculty!.Id}/departments", adminToken, new { Name = $"rds-inuse-dept-{suffix}" });
        var department = await deptResponse.Content.ReadFromJsonAsync<FacultyDto>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var studentEmail = $"rds-inuse-student-{suffix}@test.local";
            var studentUser = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(studentUser, "Student!Test123456");
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(studentUser, "Member");

            db.Students.Add(new Student
            {
                ApplicationUserId = studentUser.Id, StudentNumber = $"S{Guid.NewGuid():N}"[..12], DepartmentId = department!.Id, EnrollmentYear = 2026,
            });
            await db.SaveChangesAsync();
        }

        var deleteResponse = await SendWithBearerAsync(HttpMethod.Delete, $"/api/faculties/{faculty.Id}/departments/{department!.Id}", adminToken);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact(DisplayName = "Faz 13: PUT ile akademik dönem adı/tarihleri güncellenir")]
    public async Task UpdateAcademicTerm_ValidChange_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();

        var start = DateTime.UtcNow.AddMonths(6);
        var end = start.AddMonths(4);
        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, "/api/academic-terms", adminToken, new { Name = $"rds-term-old-{suffix}", StartDateUtc = start, EndDateUtc = end });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createdTermId = await createResponse.Content.ReadFromJsonAsync<int>();

        var newName = $"rds-term-new-{suffix}";
        var updateResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/academic-terms/{createdTermId}", adminToken, new { Name = newName, StartDateUtc = start, EndDateUtc = end });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/academic-terms?pageIndex=0&pageSize=200", adminToken);
        Assert.Contains(newName, await getResponse.Content.ReadAsStringAsync());
    }

    private async Task<string> LoginAndGetAccessTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = AdminPassword });
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

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }

    private sealed class FacultyDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
