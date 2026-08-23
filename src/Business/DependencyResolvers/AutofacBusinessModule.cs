using Autofac;
using Autofac.Extras.DynamicProxy;
using AutoMapper;
using Business.Abstract;
using Business.BackgroundJobs;
using Business.Concrete;
using Business.Mappings;
using Core.Aspects.Autofac;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Email;
using Core.Utilities.Files;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Core.Utilities.Web;
using DataAccess.DependencyResolvers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

        builder.Register(_ => Options.Create(BuildSmtpSettings(configuration)))
            .As<IOptions<SmtpSettings>>()
            .SingleInstance();

        builder.Register(_ => Options.Create(BuildFileStorageSettings(configuration)))
            .As<IOptions<FileStorageSettings>>()
            .SingleInstance();

        builder.Register(_ => Options.Create(BuildFrontendSettings(configuration)))
            .As<IOptions<FrontendSettings>>()
            .SingleInstance();

        builder.RegisterType<MemoryCacheManager>()
            .As<ICacheManager>()
            .SingleInstance();

        builder.RegisterType<SystemClock>()
            .As<IClock>()
            .SingleInstance();

        builder.RegisterType<SmtpEmailSender>()
            .As<IEmailSender>()
            .InstancePerLifetimeScope();

        builder.RegisterType<LocalFileStorage>()
            .As<IFileStorage>()
            .SingleInstance();

        builder.Register(_ =>
            {
                var mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);
                return mapperConfiguration.CreateMapper();
            })
            .As<IMapper>()
            .SingleInstance();

        // Business/ValidationRules'taki tüm FluentValidation validator'ları toplu kaydedilir.
        // .AsSelf() gerekli — ValidationAspectAttribute somut validator tipini taşır, IValidator<T>'yi değil.
        builder.RegisterAssemblyTypes(typeof(AutofacBusinessModule).Assembly)
            .AsClosedTypesOf(typeof(IValidator<>))
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<PerformanceAspectHandler>();
        builder.RegisterType<SecuredOperationAspectHandler>();
        builder.RegisterType<ValidationAspectHandler>();
        builder.RegisterType<TransactionAspectHandler>();
        builder.RegisterType<CacheAspectHandler>();
        builder.RegisterType<CacheRemoveAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();

        builder.RegisterType<DiagnosticsManager>()
            .As<IDiagnosticsService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        // A-17/Y-45: rol→izin haritası burada cache'lenir; proxy kaydı olmadan [CacheAspect] sessizce devre dışı kalır.
        builder.RegisterType<RolePermissionCatalog>()
            .As<IRolePermissionCatalog>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<IdentityGateway>()
            .As<IIdentityGateway>()
            .InstancePerLifetimeScope();

        builder.RegisterType<AccountGateway>()
            .As<IAccountGateway>()
            .InstancePerLifetimeScope();

        builder.RegisterType<IdentitySeeder>()
            .As<IIdentitySeeder>()
            .InstancePerLifetimeScope();

        // K-17: yazma yüzeyi — normalized name/ConcurrencyStamp tutarlılığı Identity API'siyle korunur, aspect taşımaz.
        builder.RegisterType<IdentityAdminGateway>()
            .As<IIdentityAdminGateway>()
            .InstancePerLifetimeScope();

        builder.RegisterType<RoleAdminManager>()
            .As<IRoleAdminService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<ReferenceDataManager>()
            .As<IReferenceDataService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<AcademicTermManager>()
            .As<IAcademicTermService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<EventManager>()
            .As<IEventService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<AuthManager>()
            .As<IAuthService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<AccountManager>()
            .As<IAccountService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<ClubManager>()
            .As<IClubService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<ClubMemberManager>()
            .As<IClubMemberService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<MembershipApplicationManager>()
            .As<IMembershipApplicationService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<ClubApplicationManager>()
            .As<IClubApplicationService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<EventParticipationManager>()
            .As<IEventParticipationService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<AnnouncementManager>()
            .As<IAnnouncementService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        // Y-51: kapsam kuralı iç bileşen — aspect taşımaz, proxy'siz kayıt.
        builder.RegisterType<ReportScopeResolver>()
            .As<IReportScopeResolver>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ExcelReportBuilder>()
            .As<IExcelReportBuilder>()
            .InstancePerLifetimeScope();

        builder.RegisterType<FileManager>()
            .As<IFileService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<ReportManager>()
            .As<IReportService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<ReportGenerationManager>()
            .As<IReportGenerationService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<DashboardManager>()
            .As<IDashboardService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<AuditLogManager>()
            .As<IAuditLogService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<PublicContentManager>()
            .As<IPublicContentService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        builder.RegisterType<MaintenanceManager>()
            .As<IMaintenanceService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        // Hangfire, iş sınıflarını kendi aktivatörü üzerinden (uygulamanın IServiceProvider'ı,
        // sonuçta Autofac tarafından destekleniyor) somut tipe göre çözer — arayüz gerekmez.
        builder.RegisterType<MembershipDecisionNotificationJob>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ClubApplicationDecisionNotificationJob>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EventDecisionNotificationJob>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EmailConfirmationJob>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PasswordResetEmailJob>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ReportGenerationJob>()
            .InstancePerLifetimeScope();

        builder.RegisterType<NightlyMaintenanceJob>()
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

    private static SmtpSettings BuildSmtpSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("Smtp");

        return new SmtpSettings
        {
            Host = section["Host"] ?? string.Empty,
            Port = int.TryParse(section["Port"], out var port) ? port : 587,
            EnableSsl = !bool.TryParse(section["EnableSsl"], out var enableSsl) || enableSsl,
            FromAddress = section["FromAddress"] ?? string.Empty,
            FromName = section["FromName"] ?? string.Empty,
            Username = section["Username"],
            Password = section["Password"],
        };
    }

    private static FrontendSettings BuildFrontendSettings(IConfiguration configuration) =>
        new() { BaseUrl = (configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/') };

    // Y-49: RootPath boşsa uygulama klasörü altında App_Data/uploads'a düşer — wwwroot dışı, secret değil.
    private static FileStorageSettings BuildFileStorageSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("FileStorage");
        var rootPath = section["RootPath"];

        return new FileStorageSettings
        {
            RootPath = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")
                : rootPath,
            MaxUploadBytes = long.TryParse(section["MaxUploadBytes"], out var maxBytes) ? maxBytes : 5 * 1024 * 1024,
        };
    }
}
