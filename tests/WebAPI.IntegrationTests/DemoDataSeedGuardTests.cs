using Business.Abstract;
using DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md · Faz 28 (Y-68): <b>demo veri üretim ortamına yazılamaz.</b>
///
/// Bu sınıf yalnızca "hiçbir şey üretilmediği" durumları sınar, bu yüzden hepsi aynı
/// veritabanını güvenle paylaşır. Üretimi sınayan testler ayrı sınıflardadır (ayrı veritabanı).
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class DemoDataSeedGuardTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DemoDataSeedGuardTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Seed:Demo kapalıyken hiçbir demo kayıt oluşmaz")]
    public async Task Seeder_WhenDisabled_CreatesNothing()
    {
        using var scope = _factory.Services.CreateScope();

        var outcome = await scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>().SeedAsync();

        Assert.Equal(DemoSeedOutcome.Disabled, outcome);
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().DemoSeedRecords.CountAsync());
    }

    [Fact(DisplayName = "Seed:Demo açık ama parola yoksa hiçbir demo kayıt oluşmaz (Y-20)")]
    public async Task Seeder_WhenPasswordMissing_CreatesNothing()
    {
        using var passwordlessFactory = WithConfiguration(new Dictionary<string, string?>
        {
            ["Seed:Demo"] = "true",
            ["Seed:DemoPassword"] = "",
        });

        using var scope = passwordlessFactory.Services.CreateScope();

        var outcome = await scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>().SeedAsync();

        Assert.Equal(DemoSeedOutcome.PasswordMissing, outcome);
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().DemoSeedRecords.CountAsync());
    }

    [Theory(DisplayName = "Anahtar açıkça 'true' değilse kapalı sayılır")]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("evet")]
    [InlineData("")]
    public async Task Seeder_WithNonBooleanFlag_StaysDisabled(string flagValue)
    {
        using var oddFlagFactory = WithConfiguration(new Dictionary<string, string?>
        {
            ["Seed:Demo"] = flagValue,
            ["Seed:DemoPassword"] = "Demo!Test123456",
        });

        using var scope = oddFlagFactory.Services.CreateScope();

        var outcome = await scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>().SeedAsync();

        Assert.Equal(DemoSeedOutcome.Disabled, outcome);
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<AppDbContext>().DemoSeedRecords.CountAsync());
    }

    private WebApplicationFactory<Program> WithConfiguration(Dictionary<string, string?> settings) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(settings)));
}
