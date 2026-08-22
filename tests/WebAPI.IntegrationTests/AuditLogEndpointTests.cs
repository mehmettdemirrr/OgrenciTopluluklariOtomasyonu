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
/// docs/PLAN-V2.md · Faz 13 (K-12): Audit ekranının kabul testi — Y-26 muhafızı, AuditSaveChangesInterceptor'ın
/// hassas alanları (parola/token/stamp) zaten hariç tuttuğu varsayımını gerçek HTTP sözleşmesine bağlar.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AuditLogEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "audit-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AuditLogEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    [Fact(DisplayName = "Y-26: ApplicationUser oluşturma (kayıt) sonrası audit kaydında parola/stamp alan adı hiç geçmez")]
    public async Task GetAuditLogs_AfterUserRegistration_NeverLeaksSensitiveFieldNames()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentEmail = $"audit-student-{suffix}@test.local";

        // Faz 11'in kayıt akışı ApplicationUser + Student satırlarını yazar — ikisi de audit'lenir.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var faculty = new Faculty { Name = $"audit-fac-{suffix}" };
            db.Faculties.Add(faculty);
            await db.SaveChangesAsync();
            var department = new Department { Name = $"audit-dept-{suffix}", FacultyId = faculty.Id };
            db.Departments.Add(department);
            await db.SaveChangesAsync();

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                Email = studentEmail,
                Password = "Str0ng!Pass1",
                StudentNumber = $"S{suffix}",
                DepartmentId = department.Id,
                EnrollmentYear = 2026,
            });
            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        }

        var adminToken = await LoginAndGetAccessTokenAsync();
        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/audit-logs?entityType=ApplicationUser&pageIndex=0&pageSize=50", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ApplicationUser", body);

        string[] forbiddenFieldNames = ["PasswordHash", "SecurityStamp", "ConcurrencyStamp", "TokenHash"];
        foreach (var forbidden in forbiddenFieldNames)
        {
            Assert.DoesNotContain(forbidden, body);
        }
    }

    [Fact(DisplayName = "audit.read taşımayan bir kullanıcı (öğrenci) denetim izini göremez — 403")]
    public async Task GetAuditLogs_WithoutAuditReadPermission_ReturnsForbidden()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        const string studentPassword = "Student!Test123456";
        var studentEmail = $"audit-noperm-{suffix}@test.local";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var student = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(student, studentPassword);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(student, "Member");
        }

        var studentToken = await LoginAndGetAccessTokenAsync(studentEmail, studentPassword);
        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/audit-logs", studentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string? email = null, string? password = null)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email ?? AdminEmail, Password = password ?? AdminPassword });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendWithBearerAsync(HttpMethod method, string requestUri, string accessToken)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
