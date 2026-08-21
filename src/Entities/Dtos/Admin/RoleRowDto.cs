namespace Entities.Dtos.Admin;

/// <summary>docs/MIMARI.md · K-17: DataAccess'in rol listesi sorgusundan projekte ettiği satır.</summary>
public sealed class RoleRowDto
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
