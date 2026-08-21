namespace Business.DTOs.Admin;

public sealed class RoleListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsSystemRole { get; set; }

    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
