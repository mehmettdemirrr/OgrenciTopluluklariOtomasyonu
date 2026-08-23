using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V4.md §19.5 (A-53/Y-63, K-13 kısmi): anonim kimlik uçlarında oran sınırı.
/// Sınır değerleri burada <c>WithWebHostBuilder</c> ile 2'ye düşürülür — temel fabrika sınırı
/// kasıtlı olarak etkisiz bırakır (bkz. CustomWebApplicationFactory).
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class RateLimitTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string UserEmail = "rl-user@test.local";
    private const string UserPassword = "User!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private WebApplicationFactory<Program> _strictFactory = null!;
    private HttpClient _client = null!;

    public RateLimitTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _strictFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:AuthStrict:PermitLimit"] = "2",
                    ["RateLimiting:AuthStrict:WindowMinutes"] = "5",
                })));

        using var scope = _strictFactory.Services.CreateScope();
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

        _client = _strictFactory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _strictFactory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "Y-63: forgot-password sınırı aşınca 429 + ProblemDetails (çıplak 429 değil)")]
    public async Task ForgotPassword_BeyondLimit_Returns429WithProblemDetails()
    {
        // Y-55: kayıtlı olsun olmasın aynı cevabı döner — sınır altındaki istekler 200.
        for (var i = 0; i < 2; i++)
        {
            var allowed = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "someone@test.local" });
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var rejected = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "someone@test.local" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        // Y-25: hata gövdesi diğer tüm hatalarla aynı formatta olmalı — boş 429 değil.
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);

        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal(429, problem!.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));

        // FixedWindowRateLimiter'ın RetryAfter meta verisi başlığa yansımalı.
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact(DisplayName = "A-53: global limiter yok — kimlik doğrulamalı uç sınırsız çağrılabiliyor")]
    public async Task AuthenticatedEndpoint_IsNeverRateLimited()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = UserEmail, Password = UserPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var token = (await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;

        // auth-strict sınırı 2; bu uç o politikayı taşımadığı için 10 istek de geçmeli.
        for (var i = 0; i < 10; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/clubs");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    private sealed class ProblemDetailsResponse
    {
        public int Status { get; init; }

        public string Title { get; init; } = string.Empty;
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
