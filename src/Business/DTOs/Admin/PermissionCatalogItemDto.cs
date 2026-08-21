namespace Business.DTOs.Admin;

public sealed class PermissionCatalogItemDto
{
    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public required string Description { get; set; }
}
