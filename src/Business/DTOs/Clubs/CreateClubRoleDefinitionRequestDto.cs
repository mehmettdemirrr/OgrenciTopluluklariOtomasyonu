using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class CreateClubRoleDefinitionRequestDto
{
    public string Name { get; set; } = string.Empty;

    public ClubRole ClubRole { get; set; }

    /// <summary>docs/PLAN-V6.md · O-27: değişirse bu unvanı taşıyan TÜM üyeliklere aynı transaction'da yayılır.</summary>
    public ClubCapability Capabilities { get; set; }

    public int DisplayOrder { get; set; }
}
