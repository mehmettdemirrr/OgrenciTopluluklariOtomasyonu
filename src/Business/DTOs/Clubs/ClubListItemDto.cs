namespace Business.DTOs.Clubs;

public sealed class ClubListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int? LogoFileId { get; set; }
}
