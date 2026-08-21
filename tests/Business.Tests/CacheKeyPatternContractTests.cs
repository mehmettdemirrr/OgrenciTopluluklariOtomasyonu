using System.Reflection;
using System.Text.RegularExpressions;
using Business.Abstract;
using Business.Concrete;
using Core.Aspects.Autofac;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/MIMARI.md · Y-45'in iki yarısını (CacheAspectHandler.BuildKey'in ürettiği gerçek anahtar ↔
/// CacheRemoveAspect'in deseni) birbirine kilitleyen tek test. BuildKey formatı
/// "{TargetType.Name}.{Method.Name}(arg1,arg2)" — TargetType.Name ileride değişirse (ör. sınıf
/// yeniden adlandırılırsa) bu test kırmızıya döner ve sessiz bir eşleşme kaybını yakalar.
/// </summary>
public class CacheKeyPatternContractTests
{
    [Fact(DisplayName = "RolePermissionCatalog: [CacheRemoveAspect] deseni, gerçek cache anahtarıyla eşleşir")]
    public void RolePermissionCatalogPattern_MatchesActualCacheKey()
    {
        var pattern = GetCacheRemovePattern(typeof(IRoleAdminService), nameof(IRoleAdminService.SetRolePermissionsAsync));
        var actualKey = BuildKey(typeof(RolePermissionCatalog), nameof(IRolePermissionCatalog.GetPermissionsAsync), "Admin");

        AssertNoRegexMetacharacters(pattern);
        Assert.Matches(pattern, actualKey);
    }

    [Fact(DisplayName = "RolePermissionCatalog: desen, başka bir sınıfın anahtarıyla eşleşmez (izin sızıntısı regresyonu)")]
    public void RolePermissionCatalogPattern_DoesNotMatchUnrelatedKey()
    {
        var pattern = GetCacheRemovePattern(typeof(IRoleAdminService), nameof(IRoleAdminService.SetRolePermissionsAsync));
        var unrelatedKey = BuildKey(typeof(ClubManager), "GetListPagedAsync", "0", "20");

        Assert.DoesNotMatch(pattern, unrelatedKey);
    }

    [Fact(DisplayName = "ReferenceDataManager: [CacheRemoveAspect] deseni, gerçek cache anahtarıyla eşleşir")]
    public void ReferenceDataManagerPattern_MatchesActualCacheKey()
    {
        var pattern = GetCacheRemovePattern(typeof(IReferenceDataService), nameof(IReferenceDataService.CreateFacultyAsync));
        var actualKey = BuildKey(typeof(ReferenceDataManager), nameof(IReferenceDataService.GetFacultiesPagedAsync), "0", "20");

        AssertNoRegexMetacharacters(pattern);
        Assert.Matches(pattern, actualKey);
    }

    private static string GetCacheRemovePattern(Type serviceInterface, string methodName)
    {
        var method = serviceInterface.GetMethod(methodName) ?? throw new InvalidOperationException($"{methodName} bulunamadı.");
        var attribute = method.GetCustomAttribute<CacheRemoveAspectAttribute>()
            ?? throw new InvalidOperationException($"{methodName} üzerinde [CacheRemoveAspect] yok.");
        return attribute.Pattern;
    }

    /// <summary>CacheAspectHandler.BuildKey ile birebir aynı format — kasıtlı olarak burada yeniden üretilir (production kodu değiştirilmeden okunur).</summary>
    private static string BuildKey(Type targetType, string methodName, params string[] args) =>
        $"{targetType.Name}.{methodName}({string.Join(",", args)})";

    // Desende (, ), [, ] asla kullanılmaz — RemoveByPattern ham (anchor'sız) Regex kullanıyor.
    private static void AssertNoRegexMetacharacters(string pattern)
    {
        foreach (var forbidden in new[] { "(", ")", "[", "]" })
        {
            Assert.DoesNotContain(forbidden, pattern, StringComparison.Ordinal);
        }
    }
}
