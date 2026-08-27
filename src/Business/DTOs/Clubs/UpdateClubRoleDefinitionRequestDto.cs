using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class UpdateClubRoleDefinitionRequestDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>docs/PLAN-V6.md · O-27: değişirse bu unvanı taşıyan TÜM üyeliklere aynı transaction'da yayılır.</summary>
    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
