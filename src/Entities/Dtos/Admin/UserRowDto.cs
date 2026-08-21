namespace Entities.Dtos.Admin;

/// <summary>docs/MIMARI.md · K-17: DataAccess'in kullanıcı listesi sorgusundan projekte ettiği satır.</summary>
public sealed class UserRowDto
{
    public int Id { get; set; }

    public required string Email { get; set; }
}
