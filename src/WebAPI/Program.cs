using Autofac;
using Autofac.Extensions.DependencyInjection;
using Business.DependencyResolvers;
using Core.Utilities.Security;
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
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<AutofacBusinessModule>();
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

app.UseAuthorization();

app.MapControllers();

app.Run();

// WebApplicationFactory<Program> ile entegrasyon testi yazabilmek için top-level statements'ın
// örtük ürettiği Program sınıfını görünür kılar.
public partial class Program { }
