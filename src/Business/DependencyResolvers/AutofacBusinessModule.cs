using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Abstract;
using Business.Concrete;
using Core.Aspects.Autofac;
using Core.CrossCuttingConcerns.Caching;

namespace Business.DependencyResolvers;

/// <summary>
/// docs/MIMARI.md · Teknoloji tablosu: "DI: Microsoft DI + Autofac ... Business modülü tanımlar".
/// Servisleri arayüz proxy'si olarak kaydeder ki aspect attribute'ları
/// <see cref="AspectDispatchInterceptor"/> üzerinden otomatik devreye girsin.
/// </summary>
public sealed class AutofacBusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MemoryCacheManager>()
            .As<ICacheManager>()
            .SingleInstance();

        builder.RegisterType<PerformanceAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();

        builder.RegisterType<DiagnosticsManager>()
            .As<IDiagnosticsService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();
    }
}
