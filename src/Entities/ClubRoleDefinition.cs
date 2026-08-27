using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-36/A-61/Y-69: kulübe özel rol UNVANI. Yetkinin kaynağı DEĞİLDİR —
/// yalnızca bir <see cref="ClubRole"/> yetki seviyesine bağlanır ve <c>ClubMembership.ClubRole</c>
/// o seviyeden yazılır. Yetki kararı veren 7 <c>Ensure*Access*</c> metodu bu tipi hiç görmez.
///
/// <b>Y-69:</b> bu sınıfa izin kodu, claim veya <c>RoleClaim</c> referansı EKLENEMEZ — Identity'nin
/// yanına ikinci bir yetki sistemi açmak yasaktır (Y-37'nin kulüp karşılığı).
/// <c>ClubRoleDefinitionShapeTests</c> ihlali yakalar.
/// </summary>
public sealed class ClubRoleDefinition : IEntity
{
    public int Id { get; set; }

    /// <summary>docs/PLAN-V6.md · O-20: rol tanımları kulübe özeldir; kulüpler birbirinin listesini görmez.</summary>
    public int ClubId { get; set; }

    /// <summary>Görünen unvan: "Sayman", "Sekreter", "Sosyal Medya Sorumlusu". <c>(ClubId, Name)</c> unique.</summary>
    public required string Name { get; set; }

    /// <summary>Bu unvanın verdiği yetki seviyesi. Değişirse üyeliklere yayılır (O-27).</summary>
    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
