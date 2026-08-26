using Entities.Enums;

namespace Business.DTOs.Reference;

public sealed class AcademicTermListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public bool IsCurrent { get; set; }

    /// <summary>docs/MIMARI.md · K-39: yönetim ekranı pencereyi bu üç alandan çizer.</summary>
    public DateTime? ClubApplicationStartUtc { get; set; }

    public DateTime? ClubApplicationEndUtc { get; set; }

    public ClubApplicationWindowOverride ClubApplicationOverride { get; set; }
}
