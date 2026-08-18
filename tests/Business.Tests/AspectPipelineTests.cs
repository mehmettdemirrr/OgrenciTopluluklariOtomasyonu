using Autofac;
using Business.Abstract;
using Business.DependencyResolvers;
using Castle.DynamicProxy;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/MIMARI.md · Faz 2 "bitti sayılır": boş bir servis metodu aspect zincirinden
/// geçip doğru sonucu üretiyor mu, gerçek bir Autofac konteyneri üzerinden kanıtlanır.
/// </summary>
public class AspectPipelineTests
{
    [Fact(DisplayName = "IDiagnosticsService, Autofac üzerinden interception uygulanmış bir proxy olarak çözülür")]
    public async Task Servis_Proxy_Olarak_Cozulur_Ve_Dogru_Sonucu_Doner()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<AutofacBusinessModule>();
        using var container = builder.Build();

        var service = container.Resolve<IDiagnosticsService>();

        Assert.True(ProxyUtil.IsProxy(service), "Servis, aspect zincirinin çalışabilmesi için bir Castle proxy'si olmalı.");

        var result = await service.PingAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("pong", result.Data);
    }
}
