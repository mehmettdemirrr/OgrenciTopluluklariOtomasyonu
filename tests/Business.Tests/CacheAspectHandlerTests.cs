using Autofac;
using Autofac.Extras.DynamicProxy;
using Core.Aspects.Autofac;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Business.Tests;

public class CacheAspectHandlerTests
{
    private sealed class CallCounter
    {
        public int Count;
    }

    public interface ITestCacheableService
    {
        [CacheAspect(durationMinutes: 5)]
        Task<IDataResult<int>> GetValueAsync(int input);
    }

    private sealed class TestCacheableService(CallCounter counter) : ITestCacheableService
    {
        public Task<IDataResult<int>> GetValueAsync(int input)
        {
            counter.Count++;
            return Task.FromResult<IDataResult<int>>(DataResult<int>.Success(input * 2));
        }
    }

    [Fact(DisplayName = "Aynı argümanla ikinci çağrı cache hit sağlar, metodu tekrar çalıştırmaz")]
    public async Task AyniArgumanlaIkinciCagri_CacheHitSaglar()
    {
        var counter = new CallCounter();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(counter);
        builder.RegisterInstance<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        builder.RegisterType<MemoryCacheManager>().As<ICacheManager>().SingleInstance();
        builder.RegisterType<CacheAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();
        builder.RegisterType<TestCacheableService>()
            .As<ITestCacheableService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        using var container = builder.Build();
        var service = container.Resolve<ITestCacheableService>();

        var first = await service.GetValueAsync(3);
        var second = await service.GetValueAsync(3);

        Assert.Equal(1, counter.Count);
        Assert.Equal(first.Data, second.Data);

        var third = await service.GetValueAsync(4);
        Assert.Equal(2, counter.Count);
        Assert.Equal(8, third.Data);
    }
}
