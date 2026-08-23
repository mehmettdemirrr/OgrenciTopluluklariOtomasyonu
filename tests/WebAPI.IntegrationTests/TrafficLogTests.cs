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
/// docs/PLAN-V3.md §18 "Çıkış koşulu": istek satırı audit değişikliğine bağlanıyor; `?access_token=...`
/// içeren istek atılır ve token veritabanında hiçbir yerde bulunmaz (Y-59); `audit.read` taşımayan
/// kullanıcı 403 alıyor.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class TrafficLogTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "tl-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string MemberEmail = "tl-member@test.local";
    private const string MemberPassword = "Member!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public TrafficLogTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Admin");
        await EnsureUserAsync(userManager, MemberEmail, MemberPassword, "Member");

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "audit.read taşımayan kullanıcı GET /api/traffic-logs'ta 403 alır")]
    public async Task GetTrafficLogs_WithoutAuditRead_ReturnsForbidden()
    {
        var memberToken = await LoginAsync(MemberEmail, MemberPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/traffic-logs", memberToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Y-59: ?access_token=... içeren istek — token veritabanında hiçbir yerde ham bulunmaz")]
    public async Task Request_WithAccessTokenInQueryString_IsRedactedInTrafficLog()
    {
        const string fakeJwt = "eyJhbGciOiJIUzI1NiJ9.FAKE-PAYLOAD-SHOULD-NEVER-BE-STORED.FAKE-SIGNATURE";

        // Yetkisiz (401) bir istek — statusCode >= 400 olduğu için loglanır (O-2).
        var response = await _client.GetAsync($"/api/traffic-logs?access_token={fakeJwt}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var row = await WaitForTrafficLogAsync(db, t => t.Path.Contains("traffic-logs") && t.StatusCode == 401);

        Assert.NotNull(row);
        Assert.DoesNotContain("FAKE-PAYLOAD", row!.RedactedQueryString ?? string.Empty);
        Assert.Contains("[REDACTED]", row.RedactedQueryString ?? string.Empty);

        // Ham JWT'nin tablonun HİÇBİR sütununda bulunmadığının doğrudan kanıtı.
        var anyRawToken = await db.TrafficLogs.AnyAsync(t => t.RedactedQueryString != null && t.RedactedQueryString.Contains("FAKE-PAYLOAD"));
        Assert.False(anyRawToken);
    }

    [Fact(DisplayName = "Tek bir PUT isteği → bir TrafficLog satırı + aynı CorrelationId'li AuditLog satır(lar)ı")]
    public async Task WriteRequest_ProducesLinkedTrafficLogAndAuditLog()
    {
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);

        int facultyId;
        var originalName = $"TL Fakülte {Guid.NewGuid():N}"[..25];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var faculty = new Faculty { Name = originalName };
            db.Faculties.Add(faculty);
            await db.SaveChangesAsync();
            facultyId = faculty.Id;
        }

        var newName = $"TL Fakülte Güncel {Guid.NewGuid():N}"[..25];
        var updateResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/faculties/{facultyId}", adminToken, new { Name = newName });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var auditRow = await WaitForAuditLogAsync(db, facultyId.ToString());
            Assert.NotNull(auditRow);
            Assert.False(string.IsNullOrWhiteSpace(auditRow!.CorrelationId));

            var trafficRow = await db.TrafficLogs.SingleOrDefaultAsync(t => t.CorrelationId == auditRow.CorrelationId);
            Assert.NotNull(trafficRow);
            Assert.Equal("PUT", trafficRow!.HttpMethod);
            Assert.Contains($"faculties/{facultyId}", trafficRow.Path);
            Assert.Equal(200, trafficRow.StatusCode);
        }
    }

    private static async Task<TrafficLog?> WaitForTrafficLogAsync(AppDbContext db, Func<TrafficLog, bool> predicate, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            var match = (await db.TrafficLogs.AsNoTracking().ToListAsync()).LastOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(100);
        }

        return null;
    }

    private static async Task<AuditLog?> WaitForAuditLogAsync(AppDbContext db, string entityId, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            var match = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == nameof(Faculty) && a.EntityId == entityId).OrderByDescending(a => a.TimestampUtc).FirstOrDefaultAsync();
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(100);
        }

        return null;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
