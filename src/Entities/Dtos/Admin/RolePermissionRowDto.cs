namespace Entities.Dtos.Admin;

/// <summary>docs/MIMARI.md · K-17: rol claim'lerinden projekte edilen (rol, izin) çifti.</summary>
public sealed class RolePermissionRowDto
{
    public int RoleId { get; set; }

    public required string Permission { get; set; }
}
