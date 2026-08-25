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
/// docs/PLAN-V5.md §24.2 (A-59/Y-65) — Faz 24'ün Hangfire kabul testi.
/// Köprü çerezi panelin iç isteklerini çözmeli, ama <b>yalnızca</b> /hangfire yolunda geçerli
/// olmalı: /api/* isteklerinde kabul edilirse K-01'in "access token çerezde taşınmaz" kararı ve
/// Y-48'in CSRF savunması sessizce delinir.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class HangfireCookieScopeTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "hf-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string MemberEmail = "hf-member@test.local";
    private const string MemberPassword = "Member!Test123456";

    private const string BridgeCookieName = "__Secure-HangfireAccess";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public HangfireCookieScopeTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    [Fact(DisplayName = "A-59: query token'la ilk istek paneli açıyor ve köprü çerezini /hangfire kapsamıyla yazıyor")]
    public async Task Dashboard_WithQueryToken_SetsScopedBridgeCookie()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        var response = await _client.GetAsync($"/hangfire?access_token={Uri.EscapeDataString(token)}");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var bridge = cookies!.Single(c => c.StartsWith(BridgeCookieName + "=", StringComparison.Ordinal));

        // Y-65'in dört şartı.
        Assert.Contains("path=/hangfire", bridge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", bridge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", bridge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", bridge, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "A-59: panelin iç istekleri query parametresi olmadan, yalnızca köprü çereziyle çözülüyor")]
    public async Task DashboardAsset_WithOnlyBridgeCookie_IsAuthenticated()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        // Panelin CSS/JS varlıkları ve iç gezinmeleri access_token taşımaz — v4.0'da 401 alan yer buydu.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/hangfire/jobs/enqueued");
        request.Headers.Add("Cookie", $"{BridgeCookieName}={token}");

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Y-65: köprü çerezi /api/* isteklerinde kabul EDİLMEZ — çerezle API'ye kimlik doğrulanamaz")]
    public async Task ApiRequest_WithBridgeCookieOnly_IsUnauthorized()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        // Tarayıcı bu çerezi Path=/hangfire yüzünden zaten göndermez; burada elle gönderilse bile
        // sunucunun onu okumaması gerekir (savunmanın ikinci katmanı).
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/clubs");
        request.Headers.Add("Cookie", $"{BridgeCookieName}={token}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "A-29/K-14: hangfire.dashboard izni olmayan kullanıcı geçerli token'la bile panele giremiyor")]
    public async Task Dashboard_WithoutPermission_IsRejected()
    {
        var token = await LoginAsync(MemberEmail, MemberPassword);

        var response = await _client.GetAsync($"/hangfire?access_token={Uri.EscapeDataString(token)}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Beklenen 401/403, gelen {(int)response.StatusCode}");
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, roleName);
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
