using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-69: topluluk içi rol tanımı izin taşıyamaz — Identity'nin yanına ikinci bir
/// yetki sistemi açmak yasaktır (Y-37'nin kulüp karşılığı). Tanım yalnızca unvan + bir ClubRole
/// yetki seviyesi tutar; yetki kararı ClubMembership.ClubRole'den okunur (A-61).
///
/// Test iki yönden bakar: <b>ad</b> (Permission/Claim/Policy/Scope/Grant içeren bir özellik) ve
/// <b>şekil</b> (ilkel/enum/string dışında bir tip — koleksiyon, RoleClaim, navigation property).
/// İkisi birden gerekli: "Yetkiler" adlı bir <c>List&lt;string&gt;</c> yalnızca şekilden yakalanır,
/// <c>int PermissionMask</c> ise yalnızca addan.
/// </summary>
public class ClubRoleDefinitionShapeTests
{
    private static readonly string[] ForbiddenNameFragments =
        ["Permission", "Claim", "Policy", "Scope", "Grant"];

    [Fact(DisplayName = "Y-69: ClubRoleDefinition izin/claim taşımaz — yalnızca ilkel alanlar ve ClubRole")]
    public void ClubRoleDefinition_Izin_Tasimaz()
    {
        var type = Assembly.Load("Entities").GetType("Entities.ClubRoleDefinition");
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.NotEmpty(properties);

        var byName = properties
            .Where(p => ForbiddenNameFragments.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Select(p => $"{p.Name} (ad)");

        var byShape = properties
            .Where(p => !IsSimpleShape(p.PropertyType))
            .Select(p => $"{p.Name} ({p.PropertyType.Name})");

        var violations = byName.Concat(byShape).ToArray();

        Assert.True(
            violations.Length == 0,
            "ClubRoleDefinition izin/claim taşıyor veya ilkel olmayan bir alan içeriyor: " +
            string.Join(", ", violations) +
            ". Bkz. docs/MIMARI.md Y-69 / A-61 — unvan yetkiden ayrıdır, izinler rol claim'lerinden yönetilir.");
    }

    /// <summary>İlkel, enum veya string (ve bunların nullable hâlleri) serbest; gerisi ihlal.</summary>
    private static bool IsSimpleShape(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive || actual.IsEnum || actual == typeof(string);
    }
}
