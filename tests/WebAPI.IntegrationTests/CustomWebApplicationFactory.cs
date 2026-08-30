using Autofac;
using Core.Utilities.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Y-20/A-19: Jwt:Key appsettings*.json'da hiç yer almaz (yalnızca
/// user-secrets/ortam değişkeni) — testler kendi signing key'ini burada sağlar. Her çalışma
/// kendi LocalDB veritabanı adını kullanır (Y-34: paylaşımlı/gerçek veritabanı yok).
/// Dikkat: WebAPI.csproj'da UserSecretsId olduğu için Development ortamında çalışan bu test host'u
/// geliştiricinin gerçek `dotnet user-secrets` değerlerini de otomatik yükler. Seed:*Email/*Password
/// anahtarları burada açıkça boşaltılır — aksi hâlde IIdentitySeeder, testlerin kendi kullanıcı
/// oluşturma kodundan ÖNCE gerçek dev parolalarıyla seed eder ve testler 401 ile başarısız olur.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string TestDatabaseName { get; } = $"OgrenciTopluluklariOtomasyonu.Tests.{Guid.NewGuid():N}";

    /// <summary>Gerçek SMTP'ye asla bağlanılmaz — Hangfire işi tarafından gönderilen e-postalar burada toplanır.</summary>
    public FakeEmailSender EmailSender { get; } = new();

    /// <summary>Y-34: fabrika başına benzersiz, izole geçici klasör — Dispose'ta silinir.</summary>
    public string FileStorageRootPath { get; } = Path.Combine(Path.GetTempPath(), "ogr-top-test", Guid.NewGuid().ToString("N"));

    public CustomWebApplicationFactory()
    {
        // Y-48: __Host-Csrf cookie'si SecurePolicy=Always ile kuruluyor (Program.cs) — TestServer'ın
        // varsayılan http tabanlı istemcisiyle HttpContext.Request.IsHttps=false kalır ve çerez hiç
        // yazılmaz (üretim/gerçek https davranışıyla tutarlı ama testi anlamsız kılar). Base address'i
        // https yapmak TestServer'a şemayı "https" olarak bildirir, gerçek dev/prod koşulunu taklit eder.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

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
                ["Seed:DemoAdvisorEmail"] = "",
                ["Seed:DemoAdvisorPassword"] = "",
                ["Seed:DemoStudentEmail"] = "",
                ["Seed:DemoStudentPassword"] = "",

                // Y-68: demo veri seeder'ı testlerde de kapalıdır. Geliştiricinin user-secrets'ında
                // Seed:Demo=true olabilir — açıkça boşaltılmazsa her test sınıfı 170+ satırlık demo
                // veriyle başlar ve sayım bekleyen testler kırılır. Açan tek yer DemoDataSeedTests'tir.
                ["Seed:Demo"] = "",
                ["Seed:ResetDemo"] = "",
                ["Seed:DemoPassword"] = "",
                ["FileStorage:RootPath"] = FileStorageRootPath,
                ["Frontend:BaseUrl"] = "https://localhost.test",

                // A-53: TestServer'da Connection.RemoteIpAddress null'dur — oran sınırı bütün
                // testleri tek "unknown" kovasında toplar ve arka arkaya login yapan mevcut
                // testler (ClubApplicationFlowTests, MembershipVerticalSliceTests...) 429 almaya
                // başlardı. Varsayılan sınır burada etkisiz kılınır; sınırı fiilen sınayan tek
                // yer RateLimitTests'tir ve orada WithWebHostBuilder ile düşük değer verilir.
                ["RateLimiting:AuthStrict:PermitLimit"] = "100000",
                ["RateLimiting:AuthLogin:PermitLimit"] = "100000",
            });
        });

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        if (Directory.Exists(FileStorageRootPath))
        {
            Directory.Delete(FileStorageRootPath, recursive: true);
        }

        DropTestDatabase();
    }

    /// <summary>
    /// Y-34 her fixture'a kendi LocalDB veritabanını veriyor, ama Dispose bunu uzun süre
    /// <b>düşürmedi</b>: geçici dosya klasörü siliniyor, veritabanı kalıyordu. Her `dotnet test`
    /// koşusu fixture sayısı kadar veritabanı bırakıyor; birikince kullanıcı profilinde onlarca
    /// GB'lık .mdf/.ldf oluyor (bir kez 79 GB'a ulaştı).
    ///
    /// Temizlik <b>en iyi çabadır</b>: hata yutulur. Bir testin sonunda veritabanı düşürülemedi
    /// diye test koşusunu kırmak, gerçek bir hatayı maskeleyen gürültü olurdu.
    /// </summary>
    private void DropTestDatabase()
    {
        try
        {
            // Havuzdaki açık bağlantılar DROP'u engeller; SINGLE_USER de geri kalanı düşürür.
            SqlConnection.ClearAllPools();

            using var connection = new SqlConnection(
                "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True;");
            connection.Open();

            using var command = connection.CreateCommand();
            // Ad GUID'den üretiliyor ama yine de parametreleştirilemeyen bir tanımlayıcı —
            // QUOTENAME ile kaçırılır (Y-25'in enjeksiyon tarafı).
            command.CommandText = """
                DECLARE @name sysname = @databaseName;
                IF DB_ID(@name) IS NOT NULL
                BEGIN
                    DECLARE @sql nvarchar(max) =
                        N'ALTER DATABASE ' + QUOTENAME(@name) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;' +
                        N'DROP DATABASE ' + QUOTENAME(@name) + N';';
                    EXEC sp_executesql @sql;
                END
                """;
            command.Parameters.Add(new SqlParameter("@databaseName", TestDatabaseName));
            command.ExecuteNonQuery();
        }
        catch (SqlException)
        {
            // Yutulur: temizlik testin sonucunu belirlemez.
        }
    }

    // IWebHostBuilder'da ConfigureContainer yok (yalnızca IHostBuilder'da var — Program.cs'in
    // builder.Host.ConfigureContainer çağrısıyla aynı metot). CreateHost, WebApplicationFactory'nin
    // gerçek Program.cs'in kendi ConfigureContainer çağrısını (AutofacBusinessModule → SmtpEmailSender)
    // zaten kuyruğa eklediği host builder'ı yakaladığı nokta — burada eklenen ikinci ConfigureContainer
    // ondan SONRA çalışır, Autofac'ın "son kayıt kazanır" kuralıyla FakeEmailSender gerçek
    // implementasyonun yerini güvenle alır.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureContainer<ContainerBuilder>((_, cb) =>
        {
            cb.RegisterInstance(EmailSender).As<IEmailSender>().SingleInstance();
        });

        return base.CreateHost(builder);
    }
}
