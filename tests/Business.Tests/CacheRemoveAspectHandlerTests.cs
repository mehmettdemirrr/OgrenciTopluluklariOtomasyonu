using Autofac;
using Autofac.Extras.DynamicProxy;
using Core.Aspects.Autofac;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using Xunit;

namespace Business.Tests;

public class CacheRemoveAspectHandlerTests
{
    private sealed class FakeCacheManager : ICacheManager
    {
        public List<string> RemovedPatterns { get; } = [];

        public bool TryGet<T>(string key, out T? value)
        {
            value = default;
            return false;
        }

        public void Add(string key, object data, int durationMinutes)
        {
        }

        public void Remove(string key)
        {
        }

        public void RemoveByPattern(string pattern) => RemovedPatterns.Add(pattern);
    }

    public interface ITestCacheRemovableService
    {
        [CacheRemoveAspect("Test.Pattern")]
        Task<IResult> WriteAsync(bool succeed);
    }

    private sealed class TestCacheRemovableService : ITestCacheRemovableService
    {
        public Task<IResult> WriteAsync(bool succeed) =>
            Task.FromResult<IResult>(succeed ? Result.Success() : Result.Conflict("çakışma"));
    }

    private static (IContainer Container, FakeCacheManager CacheManager) BuildContainer()
    {
        var cacheManager = new FakeCacheManager();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(cacheManager).As<ICacheManager>();
        builder.RegisterType<CacheRemoveAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();
        builder.RegisterType<TestCacheRemovableService>()
            .As<ITestCacheRemovableService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        return (builder.Build(), cacheManager);
    }

    [Fact(DisplayName = "Başarılı yazma sonrası desene uyan anahtarlar düşürülür")]
    public async Task BasariliYazma_DesenDusurulur()
    {
        var (container, cacheManager) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestCacheRemovableService>();
            await service.WriteAsync(succeed: true);

            Assert.Contains("Test.Pattern", cacheManager.RemovedPatterns);
        }
    }

    [Fact(DisplayName = "Başarısız yazmada cache'e dokunulmaz")]
    public async Task BasarisizYazma_CacheeDokunulmaz()
    {
        var (container, cacheManager) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestCacheRemovableService>();
            await service.WriteAsync(succeed: false);

            Assert.Empty(cacheManager.RemovedPatterns);
        }
    }
}
