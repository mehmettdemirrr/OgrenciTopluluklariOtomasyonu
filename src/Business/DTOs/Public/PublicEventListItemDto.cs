namespace Business.DTOs.Public;

/// <summary>docs/PLAN-V2.md · Faz 14 (Y-58): katılımcı listesi/öğrenci no yok — yalnızca izin verilen alanlar.</summary>
public sealed class PublicEventListItemDto
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? Capacity { get; set; }

    public int? PosterFileId { get; set; }
}
