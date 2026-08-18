using Autofac;
using Business.Abstract;
using Business.DependencyResolvers;
using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=(localdb)\\mssqllocaldb;Database=Test;Trusted_Connection=True;",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = "test-signing-key-please-replace-32-bytes+",
            })
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterModule(new AutofacBusinessModule(configuration));
        using var container = builder.Build();

        var service = container.Resolve<IDiagnosticsService>();

        Assert.True(ProxyUtil.IsProxy(service), "Servis, aspect zincirinin çalışabilmesi için bir Castle proxy'si olmalı.");

        var result = await service.PingAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("pong", result.Data);
    }
}
