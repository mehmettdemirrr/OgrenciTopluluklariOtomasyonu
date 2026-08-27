using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-29/A-45: topluluk kurma başvurusu — MembershipApplication'ın kulüp karşılığı.
/// Onayda Club oluşturulur ve CreatedClubId yazılır; başvuran öğrenci o kulübe President olarak eklenir.
/// </summary>
public sealed class ClubApplication : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int AcademicTermId { get; set; }

    public required string ProposedName { get; set; }

    public string? Description { get; set; }

    public required string Justification { get; set; }

    public int ProposedAdvisorId { get; set; }

    /// <summary>docs/MIMARI.md · K-35/A-60: önerilen kategori. Nullable — zorunlu değil.</summary>
    public int? ProposedCategoryId { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public int? ReviewedByUserId { get; set; }

    public string? ReviewNote { get; set; }

    /// <summary>Onaylandıysa oluşan kulüp — izlenebilirlik.</summary>
    public int? CreatedClubId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
