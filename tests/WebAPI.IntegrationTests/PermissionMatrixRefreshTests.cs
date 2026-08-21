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
/// docs/MIMARI.md · Faz 7 "bitti sayılır": yetki değişikliği bir sonraki refresh'te etkili
/// oluyor, cache düşüyor (Y-45). K-17 uçları (roller/izinler/kullanıcı-rol ataması) uçtan uca
/// gerçek login/refresh akışıyla kanıtlanır — token'ın kendi claim'leri değil, HTTP davranışı ölçülür.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class PermissionMatrixRefreshTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "pmx-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PermissionMatrixRefreshTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, IdentitySeedData.AdminRoleName);

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "İzin geri alındığında bir sonraki refresh yeni token'ı izinsiz üretir (Y-45 — bitti sayılır)")]
    public async Task PermissionRevoked_TakesEffectOnNextRefresh_NotOnCurrentToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleName = $"pmx-role-{suffix}";
        var targetEmail = $"pmx-target-{suffix}@test.local";
        const string targetPassword = "Target!Test123456";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var targetUser = await EnsureUserAsync(userManager, targetEmail, targetPassword, roleName: null);

        var adminToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);

        var roleId = await CreateRoleAsync(adminToken, roleName);
        var setPermissionsResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/roles/{roleId}/permissions", adminToken, new { Permissions = new[] { IdentitySeedData.Permissions.ClubsRead } });
        Assert.Equal(HttpStatusCode.OK, setPermissionsResponse.StatusCode);

        var setUserRolesResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/users/{targetUser.Id}/roles", adminToken, new { RoleNames = new[] { roleName } });
        Assert.Equal(HttpStatusCode.OK, setUserRolesResponse.StatusCode);

        // 3. Hedef kullanıcı login — ayrı bir HttpClient/çerez kavanozu kullanılır ki admin'in
        // __Host-Csrf çerezi bu istemciye sızıp antiforgery'nin yeni çerez basmasını engellemesin
        // (aynı çerez kavanozunda iki farklı kullanıcı login olursa antiforgery mevcut çerezi
        // geçerli sayıp yeniden Set-Cookie yapmaz — gerçek tarayıcıda da aynı davranış, ama testte
        // farklı kullanıcılar farklı istemci demektir). Bu çağrı ayrıca RolePermissionCatalog.
        // GetPermissionsAsync(roleName) anahtarını doldurur (cache hit'in devrede olduğunun kanıtı
        // Adım A'nın birim testinde).
        var targetClient = _factory.CreateClient();
        var targetLoginResponse = await targetClient.PostAsJsonAsync("/api/auth/login", new { Email = targetEmail, Password = targetPassword });
        Assert.Equal(HttpStatusCode.OK, targetLoginResponse.StatusCode);
        var targetLoginBody = await targetLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var oldAccessToken = targetLoginBody!.AccessToken;

        // Assert A: izin devredeyken korumalı uca erişim var.
        var beforeRevoke = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", oldAccessToken);
        Assert.Equal(HttpStatusCode.OK, beforeRevoke.StatusCode);

        // 5. Admin izni geri alır.
        var revokeResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/roles/{roleId}/permissions", adminToken, new { Permissions = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        // Assert B (kasıtlı, davranışı belgeler): aynı, hâlâ geçerli access token'ın claim'leri
        // eski izni taşımaya devam eder — bu bir kusur değil, A-10 × K-01'in 15 dk'lık penceresidir.
        var afterRevokeSameToken = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", oldAccessToken);
        Assert.Equal(HttpStatusCode.OK, afterRevokeSameToken.StatusCode);

        // 7. Refresh — yeni token, izin kataloğunu (cache'i [CacheRemoveAspect] tarafından
        // düşürülmüş) DB'den yeniden çözer.
        var newAccessToken = await RefreshAndGetAccessTokenAsync(targetClient, targetLoginResponse, targetLoginBody);

        // Assert C (bitti sayılır): yeni token izni taşımıyor.
        var afterRefresh = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", newAccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, afterRefresh.StatusCode);
    }

    [Fact(DisplayName = "Rol atandığında bir sonraki refresh yeni izni taşır (ters yön)")]
    public async Task PermissionGranted_TakesEffectOnNextRefresh_NotOnCurrentToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleName = $"pmx-grant-{suffix}";
        var targetEmail = $"pmx-grant-target-{suffix}@test.local";
        const string targetPassword = "Target!Test123456";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var targetUser = await EnsureUserAsync(userManager, targetEmail, targetPassword, roleName: null);

        var adminToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);
        var roleId = await CreateRoleAsync(adminToken, roleName);
        await SendWithBearerAsync(
            HttpMethod.Put, $"/api/roles/{roleId}/permissions", adminToken, new { Permissions = new[] { IdentitySeedData.Permissions.ClubsRead } });

        // Rol henüz atanmamışken giriş — token izinsiz. Ayrı istemci: bkz. yukarıdaki test
        // (admin'in __Host-Csrf çerezinin bu istemciye sızmaması için).
        var targetClient = _factory.CreateClient();
        var targetLoginResponse = await targetClient.PostAsJsonAsync("/api/auth/login", new { Email = targetEmail, Password = targetPassword });
        var targetLoginBody = await targetLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var oldAccessToken = targetLoginBody!.AccessToken;

        var beforeGrant = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", oldAccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, beforeGrant.StatusCode);

        var assignResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/users/{targetUser.Id}/roles", adminToken, new { RoleNames = new[] { roleName } });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        // Aynı eski token hâlâ izinsiz (claim'ler dondurulmuş).
        var afterGrantSameToken = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", oldAccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, afterGrantSameToken.StatusCode);

        var newAccessToken = await RefreshAndGetAccessTokenAsync(targetClient, targetLoginResponse, targetLoginBody);
        var afterRefresh = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs", newAccessToken);
        Assert.Equal(HttpStatusCode.OK, afterRefresh.StatusCode);
    }

    [Fact(DisplayName = "Sistem rolü silinemez")]
    public async Task DeleteRole_SystemRole_ReturnsConflict()
    {
        var adminToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);

        var response = await SendWithBearerAsync(HttpMethod.Delete, $"/api/roles/{IdentitySeedData.MemberRoleId}", adminToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Admin rolünden roles.manage izni kaldırılamaz")]
    public async Task SetRolePermissions_RemovingRolesManageFromAdmin_ReturnsConflict()
    {
        var adminToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);

        var response = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/roles/{IdentitySeedData.AdminRoleId}/permissions", adminToken,
            new { Permissions = new[] { IdentitySeedData.Permissions.ClubsRead } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Aynı adla ikinci rol oluşturulamaz")]
    public async Task CreateRole_DuplicateName_ReturnsConflict()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleName = $"pmx-dup-{suffix}";
        var adminToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);

        var first = await SendWithBearerAsync(HttpMethod.Post, "/api/roles", adminToken, new { Name = roleName });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await SendWithBearerAsync(HttpMethod.Post, "/api/roles", adminToken, new { Name = roleName });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact(DisplayName = "roles.manage izni olmayan kullanıcı yetki matrisini göremez")]
    public async Task GetRoles_WithoutRolesManage_ReturnsForbidden()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var memberEmail = $"pmx-member-{suffix}@test.local";
        const string memberPassword = "Member!Test123456";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, memberEmail, memberPassword, IdentitySeedData.MemberRoleName);

        var memberToken = await LoginAndGetAccessTokenAsync(memberEmail, memberPassword);

        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/roles", memberToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Bilinmeyen izin kodu reddedilir")]
    public async Task SetRolePermissions_UnknownPermissionCode_ReturnsBadRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleName = $"pmx-unknown-{suffix}";
        var adminToken = await LoginAndGetAccessTokenAsync(AdminEmail, AdminPassword);
        var roleId = await CreateRoleAsync(adminToken, roleName);

        var response = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/roles/{roleId}/permissions", adminToken, new { Permissions = new[] { "not.a.real.permission" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CreateRoleAsync(string adminToken, string roleName)
    {
        var response = await SendWithBearerAsync(HttpMethod.Post, "/api/roles", adminToken, new { Name = roleName });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private static async Task<string> RefreshAndGetAccessTokenAsync(HttpClient client, HttpResponseMessage loginResponse, AuthResponseDto loginBody)
    {
        var refreshCookie = ExtractCookie(loginResponse, "RefreshToken");
        var antiforgeryCookie = ExtractCookie(loginResponse, "__Host-Csrf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"RefreshToken={refreshCookie}; __Host-Csrf={antiforgeryCookie}");
        request.Headers.Add("X-XSRF-TOKEN", loginBody.CsrfToken);

        var response = await client.SendAsync(request);
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

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string password, string? roleName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));

        if (roleName is not null)
        {
            await userManager.AddToRoleAsync(user, roleName);
        }

        return user;
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
