namespace Business.DTOs.Clubs;

public sealed class CreateClubRequestDto
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int AdvisorId { get; set; }

    /// <summary>docs/MIMARI.md · A-60: opsiyonel. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }
}
