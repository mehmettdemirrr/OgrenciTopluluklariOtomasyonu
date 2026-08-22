namespace Business.DTOs.Auth;

public sealed class MeResponseDto
{
    public required string Email { get; set; }

    public required IReadOnlyCollection<string> Roles { get; set; }

    public required IReadOnlyCollection<string> Permissions { get; set; }
}
