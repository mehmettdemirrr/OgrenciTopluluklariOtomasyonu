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

/// <summary>docs/PLAN-V2.md · Faz 11: yönetici doğrudan kullanıcı oluşturur/kilitler (K-03 akışını atlar).</summary>
[Collection("WebAPI Integration Tests")]
public sealed class UserAdministrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public UserAdministrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Admin: doğrudan kullanıcı oluşturur (EmailConfirmed=true) ve yeni kullanıcı hemen giriş yapabilir")]
    public async Task CreateUser_Admin_CreatesConfirmedUserThatCanLoginImmediately()
    {
        var adminToken = await SeedAndLoginAdminAsync("admin-create");

        const string newUserEmail = "admin-created@test.local";
        const string newUserPassword = "Str0ng!Pass1";

        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, "/api/users", adminToken, new { Email = newUserEmail, Password = newUserPassword, RoleNames = new[] { "Member" } });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = newUserEmail, Password = newUserPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact(DisplayName = "Admin: bir kullanıcıyı kilitler, kilitli kullanıcı giriş yapamaz")]
    public async Task SetLockout_LockedUser_CannotLogin()
    {
        var adminToken = await SeedAndLoginAdminAsync("admin-lock");

        const string targetEmail = "to-be-locked@test.local";
        const string targetPassword = "Str0ng!Pass1";
        var userId = await SeedConfirmedMemberAsync(targetEmail, targetPassword);

        var lockResponse = await SendWithBearerAsync(HttpMethod.Put, $"/api/users/{userId}/lockout", adminToken, new { Locked = true });
        Assert.Equal(HttpStatusCode.OK, lockResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = targetEmail, Password = targetPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact(DisplayName = "Admin: kendi hesabını kilitleyemez")]
    public async Task SetLockout_OwnAccount_ReturnsConflict()
    {
        var (adminToken, adminUserId) = await SeedAndLoginAdminWithIdAsync("admin-self-lock");

        var response = await SendWithBearerAsync(HttpMethod.Put, $"/api/users/{adminUserId}/lockout", adminToken, new { Locked = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<int> SeedConfirmedMemberAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Member");

        return user.Id;
    }

    private async Task<string> SeedAndLoginAdminAsync(string suffix) => (await SeedAndLoginAdminWithIdAsync(suffix)).Token;

    private async Task<(string Token, int UserId)> SeedAndLoginAdminWithIdAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string password = "Admin!Test123456";
        var email = $"{suffix}@test.local";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, IdentitySeedData.AdminRoleName);

        var token = await LoginAndGetAccessTokenAsync(email, password);
        return (token, user.Id);
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
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
