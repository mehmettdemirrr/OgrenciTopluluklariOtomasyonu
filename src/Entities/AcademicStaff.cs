using Core.Entities;

namespace Entities;

/// <summary>docs/MIMARI.md · A-14: akademik personel profili — ApplicationUser ile 1-1.</summary>
public sealed class AcademicStaff : IEntity
{
    public int Id { get; set; }

    public int ApplicationUserId { get; set; }

    public required string Title { get; set; }

    public int DepartmentId { get; set; }
}
