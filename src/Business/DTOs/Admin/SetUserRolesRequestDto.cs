namespace Business.DTOs.Admin;

public sealed class SetUserRolesRequestDto
{
    public List<string> RoleNames { get; set; } = [];
}
