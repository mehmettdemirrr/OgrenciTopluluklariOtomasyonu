using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · A-14: öğrenci profili — ApplicationUser ile 1-1.
/// A-15: rowversion ile eşzamanlılık kontrolü.
/// </summary>
public sealed class Student : IEntity
{
    public int Id { get; set; }

    public int ApplicationUserId { get; set; }

    public required string StudentNumber { get; set; }

    public int DepartmentId { get; set; }

    public int EnrollmentYear { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
