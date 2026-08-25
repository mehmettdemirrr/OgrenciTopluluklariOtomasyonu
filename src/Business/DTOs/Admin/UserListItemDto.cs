namespace Business.DTOs.Admin;

public sealed class UserListItemDto
{
    public int Id { get; set; }

    public required string Email { get; set; }

    /// <summary>docs/MIMARI.md · A-56: ad soyad. Boşsa arayüz e-postaya düşer.</summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }

    /// <summary>docs/MIMARI.md · A-57: pasife alınmış hesap giriş yapamaz, verisi durur.</summary>
    public bool IsLockedOut { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
