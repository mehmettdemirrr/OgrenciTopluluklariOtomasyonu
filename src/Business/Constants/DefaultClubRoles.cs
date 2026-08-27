using Entities.Enums;

namespace Business.Constants;

/// <summary>
/// docs/PLAN-V6.md · O-20: yeni kulüp doğduğunda kopyalanan varsayılan unvan seti.
/// Kulüp bunları sonradan düzenleyebilir/silebilir — bu bir başlangıç noktası, kural değil.
/// Migration'daki geri doldurma SQL'i bu listeyle birebir aynı olmalıdır.
/// </summary>
public static class DefaultClubRoles
{
    public static readonly IReadOnlyList<(string Name, ClubRole Role, int DisplayOrder)> All =
    [
        ("Başkan", ClubRole.President, 1),
        ("Başkan Yardımcısı", ClubRole.Officer, 2),
        ("Sayman", ClubRole.Officer, 3),
        ("Sekreter", ClubRole.Officer, 4),
        ("Üye", ClubRole.Member, 5),
    ];
}
