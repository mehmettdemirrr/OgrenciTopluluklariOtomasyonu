using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 3 "bitti sayılır" ölçütünün gerçek 15 dk beklemeden test edilmesi için:
/// AccessTokenMinutes negatif verilir, üretilen access token anında süresi dolmuş sayılır.
/// Prod appsettings.json'da değer 15 olarak kalır — bu yalnızca test konfigürasyonudur.
/// </summary>
public sealed class ShortLivedTokenWebApplicationFactory : WebApplicationFactory<Program>
{
    public string TestDatabaseName { get; } = $"OgrenciTopluluklariOtomasyonu.Tests.{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-signing-key-please-replace-32-bytes+",
                ["Jwt:AccessTokenMinutes"] = "-1",
                ["ConnectionStrings:Default"] =
                    $"Server=(localdb)\\mssqllocaldb;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;",
                // Bkz. CustomWebApplicationFactory — gerçek dev user-secrets'ının test host'una
                // sızıp EnsureUserAsync'den önce gerçek parolayla admin seed etmesini engeller.
                ["Seed:AdminEmail"] = "",
                ["Seed:AdminPassword"] = "",
            });
        });
    }
}
