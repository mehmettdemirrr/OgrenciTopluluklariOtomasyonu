using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Abstract;
using Core.Aspects.Autofac;
using Core.CrossCuttingConcerns.Caching;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/MIMARI.md · A-17/Y-45: [CacheAspect]'in IRolePermissionCatalog üzerinde gerçekten
/// çalıştığını (proxy kaydı unutulmadığını) ve rol adına göre ayrı anahtar ürettiğini
/// (izin sızıntısı regresyonu) kanıtlar. PermissionMatrixRefreshTests bu test olmadan da
/// yeşil kalabilirdi — ikisi birlikte "cache var ve doğru anahtarlanıyor" der.
/// </summary>
public class RolePermissionCatalogCacheTests
{
    private sealed class CallCounter
    {
        public int Count;
        public readonly List<string> RequestedRoleNames = [];
    }

    private sealed class FakeRolePermissionCatalog(CallCounter counter) : IRolePermissionCatalog
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(string roleName)
        {
            counter.Count++;
            counter.RequestedRoleNames.Add(roleName);
            IReadOnlyCollection<string> result = roleName switch
            {
                "Admin" => ["roles.manage", "clubs.read"],
                "Advisor" => ["clubs.read"],
                _ => [],
            };
            return Task.FromResult(result);
        }
    }

    private static (IContainer Container, CallCounter Counter, ICacheManager CacheManager) BuildContainer()
    {
        var counter = new CallCounter();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(counter);
        builder.RegisterInstance<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        builder.RegisterType<MemoryCacheManager>().As<ICacheManager>().SingleInstance();
        builder.RegisterType<CacheAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();
        builder.RegisterType<FakeRolePermissionCatalog>()
            .As<IRolePermissionCatalog>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        var container = builder.Build();
        return (container, counter, container.Resolve<ICacheManager>());
    }

    [Fact(DisplayName = "Aynı rol adıyla ikinci çağrı cache'ten döner, kaynağa gitmez")]
    public async Task AyniRolAdiylaIkinciCagri_CacheHitSaglar()
    {
        var (container, counter, _) = BuildContainer();
        using (container)
        {
            var catalog = container.Resolve<IRolePermissionCatalog>();

            var first = await catalog.GetPermissionsAsync("Admin");
            var second = await catalog.GetPermissionsAsync("Admin");

            Assert.Equal(1, counter.Count);
            Assert.Equal(first, second);
        }
    }

    [Fact(DisplayName = "Farklı rol adları farklı cache anahtarı üretir (izin sızıntısı regresyonu)")]
    public async Task FarkliRolAdlari_FarkliAnahtarUretir()
    {
        var (container, counter, _) = BuildContainer();
        using (container)
        {
            var catalog = container.Resolve<IRolePermissionCatalog>();

            var admin = await catalog.GetPermissionsAsync("Admin");
            var advisor = await catalog.GetPermissionsAsync("Advisor");

            Assert.Equal(2, counter.Count);
            Assert.Equal(["Admin", "Advisor"], counter.RequestedRoleNames);
            Assert.NotEqual(admin, advisor);
        }
    }

    [Fact(DisplayName = "RemoveByPattern sonrası aynı rol için kaynağa yeniden gidilir")]
    public async Task RemoveByPatternSonrasi_KaynagaYenidenGidilir()
    {
        var (container, counter, cacheManager) = BuildContainer();
        using (container)
        {
            var catalog = container.Resolve<IRolePermissionCatalog>();

            await catalog.GetPermissionsAsync("Admin");
            Assert.Equal(1, counter.Count);

            cacheManager.RemoveByPattern("RolePermissionCatalog.");

            await catalog.GetPermissionsAsync("Admin");
            Assert.Equal(2, counter.Count);
        }
    }
}
