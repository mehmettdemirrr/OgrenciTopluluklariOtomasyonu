using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Abstract;
using Business.Concrete;
using Core.Aspects.Autofac;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.DependencyResolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Business.DependencyResolvers;

/// <summary>
/// docs/MIMARI.md · Teknoloji tablosu: "DI: Microsoft DI + Autofac ... Business modülü tanımlar".
/// Servisleri arayüz proxy'si olarak kaydeder ki aspect attribute'ları
/// <see cref="AspectDispatchInterceptor"/> üzerinden otomatik devreye girsin.
/// Y-01/Y-05: AppDbContext/Identity store kaydı <see cref="DataAccessAutofacModule"/>'de yaşar —
/// WebAPI'nin DataAccess'e bağımlı olmaması gereken mimari testi bu şekilde yeşil kalır.
/// </summary>
public sealed class AutofacBusinessModule(IConfiguration configuration) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(new DataAccessAutofacModule(configuration));

        // WebAPI'de IConfiguration, AutofacServiceProviderFactory köprüsüyle zaten kayıtlıdır;
        // ham ContainerBuilder ile çalışan testlerde (Business.Tests) bu köprü yok — bu yüzden
        // IdentitySeeder gibi doğrudan IConfiguration isteyen bileşenler için burada da kaydedilir.
        builder.RegisterInstance(configuration).As<IConfiguration>();

        builder.Register(_ => Options.Create(BuildJwtSettings(configuration)))
            .As<IOptions<JwtSettings>>()
            .SingleInstance();

        builder.RegisterType<MemoryCacheManager>()
            .As<ICacheManager>()
            .SingleInstance();

        builder.RegisterType<SystemClock>()
            .As<IClock>()
            .SingleInstance();

        builder.RegisterType<PerformanceAspectHandler>();
        builder.RegisterType<SecuredOperationAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();

        builder.RegisterType<DiagnosticsManager>()
            .As<IDiagnosticsService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<IdentityGateway>()
            .As<IIdentityGateway>()
            .InstancePerLifetimeScope();

        builder.RegisterType<IdentitySeeder>()
            .As<IIdentitySeeder>()
            .InstancePerLifetimeScope();

        builder.RegisterType<AuthManager>()
            .As<IAuthService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();
    }

    private static JwtSettings BuildJwtSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");

        return new JwtSettings
        {
            Issuer = section["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer eksik."),
            Audience = section["Audience"] ?? throw new InvalidOperationException("Jwt:Audience eksik."),
            Key = section["Key"] ?? throw new InvalidOperationException("Jwt:Key eksik."),
            AccessTokenMinutes = int.TryParse(section["AccessTokenMinutes"], out var accessMinutes) ? accessMinutes : 15,
            RefreshTokenDays = int.TryParse(section["RefreshTokenDays"], out var refreshDays) ? refreshDays : 7,
        };
    }
}
