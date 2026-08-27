using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class CreateClubRoleDefinitionRequestDto
{
    public string Name { get; set; } = string.Empty;

    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
