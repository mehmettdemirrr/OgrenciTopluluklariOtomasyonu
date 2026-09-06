namespace Business.DTOs.Clubs;

public sealed class CreateClubRequestDto
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int AdvisorId { get; set; }

    /// <summary>docs/MIMARI.md · K-49: en fazla 3 (validator). Boş liste = kategorisiz.</summary>
    public IReadOnlyList<int> ClubCategoryIds { get; set; } = [];
}
