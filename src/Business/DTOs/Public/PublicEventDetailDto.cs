namespace Business.DTOs.Public;

/// <summary>docs/MIMARI.md · K-46/Y-82: anonim etkinlik detayı — katılımcı listesi/öğrenci no yok, yalnızca sayı.</summary>
public sealed class PublicEventDetailDto
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public int? ClubLogoFileId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    /// <summary>docs/MIMARI.md · A-71: null ise Description düz metin olarak gösterilir.</summary>
    public string? DescriptionJson { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? Capacity { get; set; }

    public int ParticipantCount { get; set; }

    public int ViewCount { get; set; }

    public int? PosterFileId { get; set; }
}
