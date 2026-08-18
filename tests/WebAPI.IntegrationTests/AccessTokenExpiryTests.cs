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
/// docs/MIMARI.md · Faz 3 "bitti sayılır": giriş → 15 dk sonra sessiz refresh → izinsiz uçta 403.
/// Süresi dolma kısmı gerçek 15 dk beklemek yerine <see cref="ShortLivedTokenWebApplicationFactory"/>'nin
/// sıkıştırılmış Jwt:AccessTokenMinutes değeriyle uçtan uca (gerçek exp claim'iyle) doğrulanır.
/// </summary>
public sealed class AccessTokenExpiryTests : IClassFixture<ShortLivedTokenWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "admin@ogrencitoplulugu.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly ShortLivedTokenWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AccessTokenExpiryTests(ShortLivedTokenWebApplicationFactory factory) => _factory = factory;

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

    [Fact(DisplayName = "Süresi dolmuş access token reddedilir, refresh çerezle yeni bir tane üretir")]
    public async Task AccessToken_ExpiresAndIsRejected_ThenRefreshIssuesNewOne()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = AdminPassword });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(loginBody);

        // Jwt:AccessTokenMinutes=-1 ile üretilen token'ın exp claim'i geçmişte — gerçek doğrulama zinciri
        // (ValidateLifetime=true, ClockSkew=Zero) tarafından reddedilmesi gerekir.
        using var expiredRequest = new HttpRequestMessage(HttpMethod.Get, "/api/diagnostics/secure-ping");
        expiredRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        var expiredResponse = await _client.SendAsync(expiredRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);

        // Refresh, süresi dolmuş access token'dan bağımsız çalışır — yalnızca httpOnly çerezdeki
        // refresh token'a bakar (K-01: izinler her refresh'te yeniden çözülür).
        var refreshCookie = ExtractCookie(loginResponse, "RefreshToken");
        var antiforgeryCookie = ExtractCookie(loginResponse, "__Host-Csrf");

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"RefreshToken={refreshCookie}; __Host-Csrf={antiforgeryCookie}");
        refreshRequest.Headers.Add("X-XSRF-TOKEN", loginBody.CsrfToken);

        var refreshResponse = await _client.SendAsync(refreshRequest);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(refreshBody);
        Assert.False(string.IsNullOrWhiteSpace(refreshBody!.AccessToken));
    }

    private static string ExtractCookie(HttpResponseMessage response, string cookieName)
    {
        var setCookieHeader = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith($"{cookieName}=", StringComparison.Ordinal));

        var value = setCookieHeader[(cookieName.Length + 1)..];
        var separatorIndex = value.IndexOf(';');
        return separatorIndex >= 0 ? value[..separatorIndex] : value;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
