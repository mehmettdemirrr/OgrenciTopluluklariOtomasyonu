namespace Business.DTOs.Admin;

public sealed class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public IReadOnlyCollection<string> RoleNames { get; set; } = [];
}
