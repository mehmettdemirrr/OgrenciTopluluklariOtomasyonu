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

    /// <summary>docs/MIMARI.md · A-68: makam. A-39 ve dönem devri kullanır; yetki vermez.</summary>
    public ClubRole ClubRole { get; set; }

    /// <summary>docs/MIMARI.md · A-68: bu unvanın kulüp içinde yapabilecekleri.</summary>
    public ClubCapability Capabilities { get; set; }

    public int DisplayOrder { get; set; }
}
