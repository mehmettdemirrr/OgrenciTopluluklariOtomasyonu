using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-37: Identity'nin yanına paralel kimlik/yetki tablosu açılamaz (A-09).
/// </summary>
public class IdentityShapeTests
{
    private static readonly string[] ForbiddenEntityNames =
    [
        "User", "Role", "OperationClaim", "UserOperationClaim", "UserClaim", "PasswordSalt"
    ];

    [Fact(DisplayName = "Y-37: Entities katmanında Identity'e paralel bir kullanıcı/rol/izin şeması tanımlanamaz")]
    public void Entities_Identity_ye_Paralel_Sema_Icermez()
    {
        var entitiesAssembly = Assembly.Load("Entities");

        var violations = entitiesAssembly.GetTypes()
            .Where(t => t.IsClass && ForbiddenEntityNames.Contains(t.Name))
            .Select(t => t.FullName)
            .ToArray();

        Assert.True(violations.Length == 0,
            "Identity yerine paralel kimlik şeması bulundu: " + string.Join(", ", violations) +
            ". Bkz. docs/MIMARI.md A-09 / Y-37 — kullanıcı ApplicationUser üzerinden, izinler rol claim'lerinden yönetilir.");
    }
}
