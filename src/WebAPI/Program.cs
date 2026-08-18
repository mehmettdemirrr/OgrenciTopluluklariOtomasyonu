using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Business.Abstract;
using Business.DependencyResolvers;
using Core.DataAccess;
using Core.Utilities.Security;
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

builder.Services.AddControllers();
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
    });

// Y-21: uçlar varsayılan olarak anonim olamaz — anonim uçlar tek tek [AllowAnonymous] ile işaretlenir.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Y-48: refresh ucu tek çerez-tabanlı uç — antiforgery header kontrolü zorunlu.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-Csrf";
});

var app = builder.Build();

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

app.MapControllers();

// Y-13: şemanın tek kaynağı EF migration'ları — K-14 gereği ayrı bir CI/CD/dağıtım adımı V1'de
// yok, bu yüzden migration'lar başlangıçta uygulanır. A-27: izin/rol kataloğu migration'da HasData
// ile; admin kullanıcısı burada idempotent seed edilir (parola hash'i UserManager gerektirdiği için
// HasData ile üretilemez, bkz. IdentitySeeder).
using (var startupScope = app.Services.CreateScope())
{
    await startupScope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().MigrateAsync();
    await startupScope.ServiceProvider.GetRequiredService<IIdentitySeeder>().SeedAsync();
}

app.Run();

// WebApplicationFactory<Program> ile entegrasyon testi yazabilmek için top-level statements'ın
// örtük ürettiği Program sınıfını görünür kılar.
public partial class Program { }
