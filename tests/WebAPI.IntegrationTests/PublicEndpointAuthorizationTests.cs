using System.Net;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V2.md · Faz 14'ün ikinci kabul testi (Y-21 regresyonu): /api/public/* DIŞINDAKİ hiçbir
/// uç anonim erişime izin vermiyor — global fallback policy (Program.cs) hâlâ yürürlükte, yeni
/// PublicContentController'ın [AllowAnonymous]'u yalnızca kendi rotasını kapsıyor.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class PublicEndpointAuthorizationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PublicEndpointAuthorizationTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory(DisplayName = "Token gönderilmeden korumalı uçlar 401 döner (temsili örneklem)")]
    [InlineData("/api/clubs")]
    [InlineData("/api/events/upcoming")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/reports/summary")]
    [InlineData("/api/audit-logs")]
    [InlineData("/api/me")]
    [InlineData("/api/faculties")]
    [InlineData("/api/academic-terms")]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory(DisplayName = "Karşılaştırma temeli: token gönderilmeden /api/public/* uçları hâlâ 200 döner")]
    [InlineData("/api/public/clubs")]
    [InlineData("/api/public/events")]
    [InlineData("/api/public/announcements")]
    public async Task PublicEndpoint_WithoutToken_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
