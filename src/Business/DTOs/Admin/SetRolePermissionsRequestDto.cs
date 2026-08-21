namespace Business.DTOs.Admin;

public sealed class SetRolePermissionsRequestDto
{
    public List<string> Permissions { get; set; } = [];
}
