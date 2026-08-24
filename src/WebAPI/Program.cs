using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Business.Abstract;
using Business.BackgroundJobs;
using Business.DependencyResolvers;
using Core.DataAccess;
using Core.Utilities.Security;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using WebAPI.Middleware;
using WebAPI.Security;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// A-04/A-09: Autofac, Castle DynamicProxy tabanlı async-farkında aspect zincirini kurabilmek için
// varsayılan Microsoft DI konteynerinin yerini alır (docs/MIMARI.md · Bölüm 4).
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
{
    containerBuilder.RegisterModule(new AutofacBusinessModule(context.Configuration));
});

// A-18: Serilog ayrı bir bağlantı üzerinden MSSQL'e yazar; iş transaction'ından bağımsızdır (Y-43).
// ConnectionStrings:LogDb boşken yalnızca konsola yazar (yerel geliştirmede SQL Server şartı yok).
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var logDbConnectionString = context.Configuration.GetConnectionString("LogDb");
    if (!string.IsNullOrWhiteSpace(logDbConnectionString))
    {
        loggerConfiguration.WriteTo.MSSqlServer(
            connectionString: logDbConnectionString,
            sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true });
    }
});

// Add services to the container.

// Sessiz onay: enum'lar API'de metin olarak taşınır (DB'de int kalır).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<ICorrelationContext, HttpContextCorrelationContext>();
// A-54: sınırlı önbellek. A-50 ile cache anahtarına serbest metin (search) girdiği için, oran sınırı
// olmayan anonim /api/public/* ucundan rastgele arama üreterek belleği şişirmek mümkün olurdu.
builder.Services.AddMemoryCache(options =>
    options.SizeLimit = builder.Configuration.GetValue<long?>("Caching:SizeLimit") ?? 2048);

// Faz 19.0 (K-28/A-53 ön koşulu): ters proxy arkasında gerçek istemci IP'si. Bu olmadan hem trafik
// logu (RequestLoggingMiddleware) hem oran sınırı proxy'nin tek IP'sini görür — biri değersizleşir,
// diğeri tüm kullanıcıları aynı kovaya koyup kilitler.
// Varsayılan KnownProxies = loopback: aynı makinedeki IIS/nginx güvenilir, uzaktaki istemcinin
// uydurduğu X-Forwarded-For **yok sayılır** (IP sahteciliği engellenir). Farklı makinedeki proxy
// appsettings'ten eklenir. Delege içinde okumak kasıtlı — bkz. aşağıdaki JwtSettings notu.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }
});

// A-53/Y-63 (K-13 kısmi): oran sınırı YALNIZCA anonim kimlik uçlarında. Global limiter kasıtlı
// olarak yok — SPA'nın meşru trafiğini boğar ve /api/public/* zaten 10 dk cache ile hafifletilmiş.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.AuthStrict, httpContext =>
        BuildIpFixedWindow(httpContext, builder.Configuration, "RateLimiting:AuthStrict", defaultPermit: 5, defaultWindowMinutes: 15));

    options.AddPolicy(RateLimitPolicies.AuthLogin, httpContext =>
        BuildIpFixedWindow(httpContext, builder.Configuration, "RateLimiting:AuthLogin", defaultPermit: 10, defaultWindowMinutes: 5));

    // Y-25: çıplak 429 değil — hata gövdesi diğer tüm hatalarla aynı formatta (ProblemDetails).
    options.OnRejected = RateLimitRejectionHandler.HandleAsync;
});

// K-01/Y-38: access token 15 dk, ClockSkew=Zero — varsayılan 5 dk tolerans olmasaydı
// token'ı sessizce ~20 dk'ya uzatırdı. Okuma IOptions ile DI zamanına ertelenir (Configure<IConfiguration>) —
// builder.Configuration'ı burada, top-level deyimlerde doğrudan okumak WebApplicationFactory'nin test
// konfigürasyonu enjeksiyonundan (ConfigureWebHost, Build() anında araya girer) önce çalışır ve testte
// her zaman "eksik" görünür.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException(
            "Jwt:Key eksik. Dev'de 'dotnet user-secrets set \"Jwt:Key\" \"...\"' ile, prod'da ortam değişkeniyle sağlanmalı (Y-20).");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // K-14: Hangfire paneli V1'in tek izleme aracı, ama access token sessionStorage'da
        // (yalnızca header) taşındığı için düz tarayıcı navigasyonu Authorization header'ı
        // göndermez. SignalR'da da kullanılan bilinen köprü deseni: yalnızca /hangfire yoluna
        // özel query string'den token okunur. Bilinçli taviz: kısa ömürlü token URL/loglarda görünebilir.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/hangfire") &&
                    context.Request.Query.TryGetValue("access_token", out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    });

// Y-21: uçlar varsayılan olarak anonim olamaz — anonim uçlar tek tek [AllowAnonymous] ile işaretlenir.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Y-48: refresh ucu tek çerez-tabanlı uç — antiforgery header kontrolü zorunlu.
// SecurePolicy açıkça Always: varsayılan SameAsRequest'e bırakılırsa (canlı HTTPS isteğinde bile
// gözlemlendi — kök neden netleşmedi) Set-Cookie'de "secure" hiç görünmeyebiliyor; __Host- önekinin
// gerektirdiği koşullardan biri (Secure + Path=/ + Domain yok) eksik kaldığında tarayıcı çerezi
// sessizce reddediyor ve /auth/refresh'e giden HER istek CSRF doğrulamasından döner.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-Csrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// K-08/Y-13: tek veritabanı — Hangfire kendi şemasını aynı DB'de kendi kurar, ayrı migration
// gerekmez. UseSqlServerStorage burada ÇAĞRILMAZ: AddHangfire'ın konfigürasyon delege'si (sp, cfg)
// aşırı yüklemesi senkron ve erken (Build()'ten önce) çalışıyor — bu, veritabanı EF migration'ı
// (aşağıda, Build() sonrası) henüz oluşturmadan Hangfire'ın bağlanmaya çalışmasına ve şema
// kurulumunun sessizce başarısız olmasına yol açıyordu (test ortamında veritabanı gerçekten yeni
// olduğu için ortaya çıkan gerçek bir hata). Depolama, migration'dan SONRA GlobalConfiguration
// üzerinden elle bağlanır; AddHangfire burada yalnızca IBackgroundJobClient gibi çekirdek
// servisleri DI'a kaydeder.
builder.Services.AddHangfire(_ => { });
builder.Services.AddHangfireServer();

var app = builder.Build();

// Y-13: şemanın tek kaynağı EF migration'ları — K-14 gereği ayrı bir CI/CD/dağıtım adımı V1'de
// yok, bu yüzden migration'lar başlangıçta uygulanır. A-27: izin/rol kataloğu migration'da HasData
// ile; admin kullanıcısı burada idempotent seed edilir (parola hash'i UserManager gerektirdiği için
// HasData ile üretilemez, bkz. IdentitySeeder). Bu blok, HTTP pipeline'ı (özellikle
// UseHangfireDashboard, JobStorage'ı hemen DI'dan çözmeye çalışır) kurulmadan ÖNCE çalışmalı.
using (var startupScope = app.Services.CreateScope())
{
    await startupScope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().MigrateAsync();
    await startupScope.ServiceProvider.GetRequiredService<IIdentitySeeder>().SeedAsync();

    // Hangfire.AspNetCore normalde LogProvider'ı kendi IHostedService'i (host Start() olduğunda,
    // yani buradan SONRA) üzerinden ayarlar. UseSqlServerStorage'ı migration'dan hemen sonra, host
    // henüz Start() olmadan çağırdığımız için LogProvider ya hiç kurulmamış olur ya da (testte aynı
    // process'te art arda kurulan önceki bir WebApplicationFactory'den kalma, artık dispose edilmiş
    // bir ILoggerFactory'e bağlı) bayat bir referans taşır — Initialize() ilk log çağrısında
    // ObjectDisposedException fırlatır. Mevcut scope'un TAZE ILoggerFactory'siyle burada elle,
    // erkenden set etmek hem üretimde (Serilog'a akış) hem testte (her factory kendi loglayıcısını
    // kaydeder) doğru davranışı garanti eder.
    Hangfire.Logging.LogProvider.SetCurrentLogProvider(
        new Hangfire.AspNetCore.AspNetCoreLogProvider(startupScope.ServiceProvider.GetRequiredService<ILoggerFactory>()));

    // Veritabanı artık var (migration az önce tamamlandı) — Hangfire'ın kendi şemasını
    // güvenle kurabileceği/bağlanabileceği an burası.
    GlobalConfiguration.Configuration.UseSqlServerStorage(
        startupScope.ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default"),
        new Hangfire.SqlServer.SqlServerStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(1) });

    // K-08/§7: gecelik tek yinelenen iş — süresi geçmiş refresh token'lar + eskimiş rapor dosyaları.
    startupScope.ServiceProvider.GetRequiredService<IRecurringJobManager>().AddOrUpdate<NightlyMaintenanceJob>(
        "gecelik-bakim",
        job => job.RunAsync(),
        "30 3 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

// Faz 19.0: her şeyden ÖNCE — bundan sonraki hiçbir bileşen (trafik logu, oran sınırı, Serilog)
// proxy'nin IP'sini gerçek istemci sanmasın.
app.UseForwardedHeaders();

// Y-25 / Y-28: beklenmeyen hatalar tek yerden, controller'larda try/catch olmadan yönetilir.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// K-28/A-44: UseAuthentication/UseAuthorization'dan ÖNCE — yetkisiz (401/403) istekler authorization
// middleware'inde kısa devre yapıp next()'i hiç çağırmaz; bu middleware SARMALAYICI olmazsa o istekler
// hiç loglanmaz (O-2 tam olarak bunları istiyor). UseAuthentication yine de önce çalışıp context.User'ı
// doldurduğu için kullanıcı kimliği burada doğru okunur — yalnızca UseAuthorization'ın kararı (ve varsa
// asıl endpoint'in çalışması) next() döndükten sonra görülür.
app.UseMiddleware<RequestLoggingMiddleware>();

// A-53 × A-44: oran sınırı erişim izinden SONRA — aksi hâlde reddedilen (429) istekler hiç
// loglanmaz. O-2 zaten "StatusCode >= 400 kaydedilir" diyor; sıra ters olsaydı Faz 18'in
// UseAuthorization tuzağı birebir tekrarlanırdı.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()],
});

app.MapControllers();

app.Run();

/// <summary>
/// A-53: IP başına sabit pencere. Sınır değerleri appsettings'ten okunur (secret değil, ayar —
/// Y-20 kapsamı dışında) ki test ortamında düşürülebilsin. IP çözülemezse tüm anonim istekler
/// tek "unknown" kovasında toplanır — proxy arkasında bunun olmaması Faz 19.0'ın işi.
/// </summary>
static RateLimitPartition<string> BuildIpFixedWindow(
    HttpContext httpContext, IConfiguration configuration, string sectionKey, int defaultPermit, int defaultWindowMinutes)
{
    var section = configuration.GetSection(sectionKey);
    var permitLimit = section.GetValue<int?>("PermitLimit") ?? defaultPermit;
    var windowMinutes = section.GetValue<int?>("WindowMinutes") ?? defaultWindowMinutes;

    var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromMinutes(windowMinutes),
        QueueLimit = 0,
    });
}

// WebApplicationFactory<Program> ile entegrasyon testi yazabilmek için top-level statements'ın
// örtük ürettiği Program sınıfını görünür kılar.
public partial class Program { }
