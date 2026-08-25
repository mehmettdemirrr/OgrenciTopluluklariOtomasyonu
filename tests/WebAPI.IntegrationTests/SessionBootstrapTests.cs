using System.Net;
using System.Net.Http.Json;
using DataAccess;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md §24.1 (A-59) — Faz 24'ün oturum kabul testi.
/// Sayfa yenilendiğinde bellekteki access token VE csrf istek token'ı birlikte kaybolur; oturumun
/// geri gelebilmesi için istemcinin sunucudan yeni bir csrf çifti alıp refresh'i onunla çağırabilmesi
/// gerekir. Bu uç olmadan "sessiz refresh" daha ilk adımda başarısız oluyordu.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class SessionBootstrapTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string UserEmail = "bootstrap-user@test.local";
    private const string UserPassword = "Bootstrap!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public SessionBootstrapTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(UserEmail) is null)
        {
            var user = new ApplicationUser { UserName = UserEmail, Email = UserEmail, EmailConfirmed = true };
            var created = await userManager.CreateAsync(user, UserPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, "Member");
        }

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-59: F5 taklidi — bellekteki her şey kaybolsa bile refresh çereziyle oturum geri geliyor")]
    public async Task Refresh_AfterLosingInMemoryTokens_RestoresSession()
    {
        // Fabrika istemcisi çerezleri tarayıcı gibi saklar (HandleCookies varsayılan true) —
        // "sayfa yenilendi" durumu tam olarak budur: çerezler durur, BELLEK sıfırlanır.
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = UserEmail, Password = UserPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // 1. Adım — istemci yeni bir csrf istek token'ı alır (bu uç Faz 24'te eklendi).
        var csrfResponse = await _client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);

        var csrfToken = (await csrfResponse.Content.ReadFromJsonAsync<CsrfResponse>())!.CsrfToken;
        Assert.False(string.IsNullOrWhiteSpace(csrfToken));

        // 2. Adım — refresh: çerezleri istemci taşır, istek token'ı başlıkla gider.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);

        var refresh = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var restored = (await refresh.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.False(string.IsNullOrWhiteSpace(restored.AccessToken));
    }

    [Fact(DisplayName = "A-59: csrf ucu anonim erişilebilir ve her çağrıda çereziyle eşleşen bir istek token'ı üretir")]
    public async Task CsrfEndpoint_IsAnonymous_AndIssuesMatchingPair()
    {
        var response = await _client.GetAsync("/api/auth/csrf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await response.Content.ReadFromJsonAsync<CsrfResponse>())!.CsrfToken));

        // Y-48: istek token'ı tek başına işe yaramaz — yanına çerez token'ı da yazılmış olmalı.
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("__Host-Csrf=", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Y-48 korunur: csrf başlığı olmadan refresh reddedilir — yeni uç CSRF savunmasını gevşetmiyor")]
    public async Task Refresh_WithoutCsrfHeader_IsRejected()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = UserEmail, Password = UserPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Çerezler istemcide duruyor ama X-XSRF-TOKEN başlığı yok — çift gönderim eşleşmesi kurulamaz.
        var response = await _client.PostAsync("/api/auth/refresh", null);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class CsrfResponse
    {
        public string CsrfToken { get; init; } = string.Empty;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
