namespace Business.DTOs.Clubs;

public sealed class ClubDetailDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int AdvisorId { get; set; }

    public int? LogoFileId { get; set; }
}
