namespace Business.DTOs.Admin;

public sealed class UserListItemDto
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
