using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · A-12/K-03: üyelik başvurusu, onay/ret, soft delete.
/// Onay/ret bildirimi Hangfire kuyruğuna eklenir (Faz 5).
/// </summary>
public sealed class MembershipApplication : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public int StudentId { get; set; }

    public int AcademicTermId { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public int? ReviewedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
