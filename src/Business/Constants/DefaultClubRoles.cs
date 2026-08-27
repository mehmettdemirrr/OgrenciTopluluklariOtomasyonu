using Entities.Enums;

namespace Business.Constants;

/// <summary>
/// docs/PLAN-V6.md · O-20: yeni kulüp doğduğunda kopyalanan varsayılan unvan seti.
/// Kulüp bunları sonradan düzenleyebilir/silebilir — bu bir başlangıç noktası, kural değil.
/// A-68: her unvanın kapasitesi makamının varsayılanıyla başlar; kulüp sonradan daraltır/genişletir.
/// Migration'daki geri doldurma SQL'i bu listeyle birebir aynı olmalıdır.
/// </summary>
public static class DefaultClubRoles
{
    public static readonly IReadOnlyList<(string Name, ClubRole Role, ClubCapability Capabilities, int DisplayOrder)> All =
    [
        ("Başkan", ClubRole.President, ClubCapabilityDefaults.ForRole(ClubRole.President), 1),
        ("Başkan Yardımcısı", ClubRole.Officer, ClubCapabilityDefaults.ForRole(ClubRole.Officer), 2),
        ("Sayman", ClubRole.Officer, ClubCapabilityDefaults.ForRole(ClubRole.Officer), 3),
        ("Sekreter", ClubRole.Officer, ClubCapabilityDefaults.ForRole(ClubRole.Officer), 4),
        ("Üye", ClubRole.Member, ClubCapabilityDefaults.ForRole(ClubRole.Member), 5),
    ];
}
