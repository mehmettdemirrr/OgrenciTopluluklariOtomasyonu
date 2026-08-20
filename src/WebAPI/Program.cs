using System.Text;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Business.Abstract;
using Business.DependencyResolvers;
using Core.DataAccess;
using Core.Utilities.Security;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
builder.Services.AddMemoryCache();

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
}

// Y-25 / Y-28: beklenmeyen hatalar tek yerden, controller'larda try/catch olmadan yönetilir.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()],
});

app.MapControllers();

app.Run();

// WebApplicationFactory<Program> ile entegrasyon testi yazabilmek için top-level statements'ın
// örtük ürettiği Program sınıfını görünür kılar.
public partial class Program { }
