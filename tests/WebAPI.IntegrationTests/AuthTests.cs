using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Business.Abstract;
using Business.DTOs.Auth;
using DataAccess;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 3 "bitti sayılır": giriş → 15 dk sonra sessiz refresh → izinsiz uçta 403.
/// Y-34: her test kendi verisini kurar; fixture başına benzersiz LocalDB veritabanı kullanılır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AuthTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "admin@ogrencitoplulugu.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string MemberEmail = "member@ogrencitoplulugu.local";
    private const string MemberPassword = "Member!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, IdentitySeedData.AdminRoleName);
        await EnsureUserAsync(userManager, MemberEmail, MemberPassword, IdentitySeedData.MemberRoleName);

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Login: doğru kimlik bilgileriyle access token döner ve refresh çerezi set edilir")]
    public async Task Login_ValidCredentials_ReturnsAccessTokenAndSetsRefreshCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("RefreshToken=", StringComparison.Ordinal));

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.CsrfToken));
    }

    [Fact(DisplayName = "Login: bilinmeyen e-posta ve yanlış parola aynı jenerik 401 mesajını döner (enumeration önleme)")]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var unknownEmailResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "yok@ogrencitoplulugu.local", Password = "herhangi" });
        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = "yanlis-parola" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
    }

    [Fact(DisplayName = "SecurePing: token olmadan 401 döner")]
    public async Task SecurePing_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/diagnostics/secure-ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "SecurePing: izinsiz kullanıcı token'ıyla 403 döner")]
    public async Task SecurePing_WithoutPermission_Returns403()
    {
        var accessToken = await LoginAndGetAccessTokenAsync(MemberEmail, MemberPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/diagnostics/secure-ping", accessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "SecurePing: izinli kullanıcı token'ıyla 200 döner")]
    public async Task SecurePing_WithPermission_Returns200()
    {
        var accessToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/diagnostics/secure-ping", accessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Refresh: CSRF header olmadan 403 döner (Y-48)")]
    public async Task Refresh_WithoutCsrfHeader_Returns403()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = AdminPassword });
        var refreshCookie = ExtractCookie(loginResponse, "RefreshToken");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"RefreshToken={refreshCookie}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Refresh: doğru çerez + CSRF header ile başarılı olur ve yeni access token döner")]
    public async Task Refresh_WithCsrfHeader_Succeeds()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = AdminPassword });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var refreshCookie = ExtractCookie(loginResponse, "RefreshToken");
        var antiforgeryCookie = ExtractCookie(loginResponse, "__Host-Csrf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"RefreshToken={refreshCookie}; __Host-Csrf={antiforgeryCookie}");
        request.Headers.Add("X-XSRF-TOKEN", loginBody!.CsrfToken);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Not: aynı saniye içinde login+refresh yapılırsa claim'ler (izinler, saniye hassasiyetli
        // exp/nbf) birebir aynı olduğundan HMAC imzası da deterministik olarak aynı token string'ini
        // üretebilir — bu güvenlik açısından zararsızdır. Asıl doğrulanan davranış refresh token'ın
        // tek kullanımlık rotasyonudur (bkz. Refresh_RotatesToken_OldTokenNowRejected), bu yüzden
        // burada yalnızca isteğin başarılı olduğu ve geçerli bir access token döndüğü kontrol edilir.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }

    [Fact(DisplayName = "Refresh: rotasyon sonrası eski refresh token reddedilir (Y-38 — tek kullanımlık)")]
    public async Task Refresh_RotatesToken_OldTokenNowRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var login = await authService.LoginAsync(new LoginRequestDto { Email = AdminEmail, Password = AdminPassword });
        Assert.True(login.IsSuccess);

        var firstRefresh = await authService.RefreshAsync(login.Data.RefreshToken);
        Assert.True(firstRefresh.IsSuccess);

        var secondAttemptWithOldToken = await authService.RefreshAsync(login.Data.RefreshToken);
        Assert.False(secondAttemptWithOldToken.IsSuccess);
    }

    [Fact(DisplayName = "Logout: refresh token'ı iptal eder, sonraki refresh başarısız olur")]
    public async Task Logout_RevokesToken_SubsequentRefreshFails()
    {
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var login = await authService.LoginAsync(new LoginRequestDto { Email = AdminEmail, Password = AdminPassword });
        Assert.True(login.IsSuccess);

        await authService.LogoutAsync(login.Data.RefreshToken);

        var afterLogout = await authService.RefreshAsync(login.Data.RefreshToken);
        Assert.False(afterLogout.IsSuccess);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
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
