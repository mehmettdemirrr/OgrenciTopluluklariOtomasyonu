using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Y-20/A-19: Jwt:Key appsettings*.json'da hiç yer almaz (yalnızca
/// user-secrets/ortam değişkeni) — testler kendi signing key'ini burada sağlar. Her çalışma
/// kendi LocalDB veritabanı adını kullanır (Y-34: paylaşımlı/gerçek veritabanı yok).
/// Dikkat: WebAPI.csproj'da UserSecretsId olduğu için Development ortamında çalışan bu test host'u
/// geliştiricinin gerçek `dotnet user-secrets` değerlerini de otomatik yükler. Seed:AdminEmail/
/// AdminPassword burada açıkça boşaltılır — aksi hâlde IIdentitySeeder, testin kendi
/// EnsureUserAsync'inden ÖNCE gerçek dev parolasıyla admin'i seed eder ve testin sabit test
/// parolasıyla girişi 401 ile başarısız olur.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string TestDatabaseName { get; } = $"OgrenciTopluluklariOtomasyonu.Tests.{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-signing-key-please-replace-32-bytes+",
                ["ConnectionStrings:Default"] =
                    $"Server=(localdb)\\mssqllocaldb;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;",
                ["Seed:AdminEmail"] = "",
                ["Seed:AdminPassword"] = "",
            });
        });
    }
}
