namespace Entities.Dtos.Admin;

/// <summary>docs/MIMARI.md · K-17: kullanıcı-rol atamasından projekte edilen (kullanıcı, rol adı) çifti.</summary>
public sealed class UserRoleRowDto
{
    public int UserId { get; set; }

    public required string RoleName { get; set; }
}
