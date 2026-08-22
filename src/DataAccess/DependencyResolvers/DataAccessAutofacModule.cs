using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core.DataAccess;
using DataAccess.Interceptors;
using DataAccess.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.DependencyResolvers;

/// <summary>
/// docs/MIMARI.md · Y-01/Y-05: "WebAPI, DataAccess katmanını atlayarak kullanamaz" mimari testinin
/// yeşil kalabilmesi için AppDbContext/Identity store kaydı burada yaşar, WebAPI'de değil.
/// Business.DependencyResolvers.AutofacBusinessModule bu modülü kaydeder (Business→DataAccess izinli).
/// </summary>
public sealed class DataAccessAutofacModule(IConfiguration configuration) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var services = new ServiceCollection();

        // K-12: audit interceptor'ı ICurrentUser'a (scoped) bağımlı olduğu için sp üzerinden,
        // her AppDbContext örneği kurulduğunda o anki scope'tan çözülür.
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseSqlServer(configuration.GetConnectionString("Default"))
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        // Faz 11 · K-03: e-posta doğrulama/parola sıfırlama token'ları için sağlayıcı gerekir
        // (UserManager.GenerateEmailConfirmationTokenAsync/GeneratePasswordResetTokenAsync bunsuz
        // "No IUserTwoFactorTokenProvider... registered" ile patlar). AddDefaultTokenProviders()
        // (DataProtectorTokenProvider) yalnızca tam Microsoft.AspNetCore.Identity paketinde var —
        // o paket FrameworkReference gerektirir (bkz. AuthManager'ın SignInManager kullanmama gerekçesi,
        // aynı kısıt). Microsoft.Extensions.Identity.Core'da bulunan EmailTokenProvider (SecurityStamp
        // tabanlı, zaman sınırlı) FrameworkReference'sız aynı işi görür — resmî Identity şablonlarının
        // e-posta doğrulama/parola sıfırlama için zaten kullandığı sağlayıcının aynısı.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddTokenProvider<EmailTokenProvider<ApplicationUser>>(TokenOptions.DefaultEmailProvider);

        builder.Populate(services);

        builder.Register(ctx => ctx.Resolve<AppDbContext>())
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.RegisterGeneric(typeof(EfEntityRepositoryBase<>))
            .As(typeof(IEntityRepository<>))
            .InstancePerLifetimeScope();

        builder.RegisterType<EfDatabaseMigrator>()
            .As<IDatabaseMigrator>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfReportDal>()
            .As<IReportDal>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfDashboardDal>()
            .As<IDashboardDal>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfIdentityAdminDal>()
            .As<IIdentityAdminDal>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfAcademicStaffDal>()
            .As<IAcademicStaffDal>()
            .InstancePerLifetimeScope();
    }
}
