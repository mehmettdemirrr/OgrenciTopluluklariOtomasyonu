using Entities.Enums;

namespace Business.DTOs.Clubs;

/// <summary>
/// docs/MIMARI.md · K-36/A-61: unvan + verdiği yetki seviyesi. <c>ClubRole</c> arayüzde yardımcı metin
/// olarak gösterilir — "Sayman" unvanını veren kişi Officer yetkisi verdiğini GÖRMEDEN vermemeli.
/// <c>ClubId</c> taşınmaz: liste zaten kulüp rotasından geliyor.
/// </summary>
public sealed class ClubRoleDefinitionDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
