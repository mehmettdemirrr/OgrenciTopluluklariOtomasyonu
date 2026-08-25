namespace Entities.Dtos.Admin;

/// <summary>docs/MIMARI.md · K-17: DataAccess'in kullanıcı listesi sorgusundan projekte ettiği satır.</summary>
public sealed class UserRowDto
{
    public int Id { get; set; }

    public required string Email { get; set; }

    /// <summary>A-56: boş olabilir — mevcut hesaplarda ad soyad yok, arayüz e-postaya düşer.</summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }

    /// <summary>A-57: "pasif" = Identity lockout'u geleceğe kurulmuş demektir.</summary>
    public bool IsLockedOut { get; set; }
}
