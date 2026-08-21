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
/// docs/MIMARI.md · Faz 7 §0.4: IX_AcademicTerms_IsCurrent filtreli unique index'i hiçbir zaman
/// ihlal etmeden dönem geçişi. Bu sınıftaki hiçbir fact başka bir test sınıfının veritabanını
/// paylaşmaz (IClassFixture: sınıf başına ayrı LocalDB) ve dönem oluşturma/güncelleme yapan tüm
/// senaryolar kasıtlı olarak burada toplanır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AcademicTermManagementTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "atm-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AcademicTermManagementTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    [Fact(DisplayName = "Yeni dönem oluşturulup güncel yapıldığında yalnızca bir güncel dönem kalır")]
    public async Task SetCurrent_NewTerm_ExactlyOneCurrentTermRemains()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();

        var termId = await CreateTermAsync(adminToken, $"atm-term-{suffix}");

        var setCurrentResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/academic-terms/{termId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, setCurrentResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AcademicTerms.CountAsync(t => t.IsCurrent));
        Assert.True((await db.AcademicTerms.SingleAsync(t => t.Id == termId)).IsCurrent);
    }

    [Fact(DisplayName = "Zaten güncel olan dönem tekrar güncel yapılırsa idempotent 200 döner")]
    public async Task SetCurrent_AlreadyCurrent_IsIdempotent()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();
        var termId = await CreateTermAsync(adminToken, $"atm-idem-{suffix}");

        var first = await SendWithBearerAsync(HttpMethod.Put, $"/api/academic-terms/{termId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await SendWithBearerAsync(HttpMethod.Put, $"/api/academic-terms/{termId}/current", adminToken);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AcademicTerms.CountAsync(t => t.IsCurrent));
    }

    [Fact(DisplayName = "Aynı adla ikinci dönem oluşturulamaz")]
    public async Task CreateTerm_DuplicateName_ReturnsConflict()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();
        var name = $"atm-dup-{suffix}";

        await CreateTermAsync(adminToken, name);

        var second = await SendWithBearerAsync(
            HttpMethod.Post, "/api/academic-terms", adminToken,
            new { Name = name, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact(DisplayName = "Bitiş tarihi başlangıçtan önce veya eşitse 400 döner")]
    public async Task CreateTerm_InvalidDateRange_ReturnsBadRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetAccessTokenAsync();
        var start = DateTime.UtcNow;

        var response = await SendWithBearerAsync(
            HttpMethod.Post, "/api/academic-terms", adminToken,
            new { Name = $"atm-invalid-{suffix}", StartDateUtc = start, EndDateUtc = start });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CreateTermAsync(string adminToken, string name)
    {
        var response = await SendWithBearerAsync(
            HttpMethod.Post, "/api/academic-terms", adminToken,
            new { Name = name, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<int>();
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
}
