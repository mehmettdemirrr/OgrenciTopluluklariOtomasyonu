namespace Business.DTOs.Reference;

/// <summary>Yönetici ucu (`reference.manage`) — e-posta burada KALIR, yönetimin ihtiyacı.</summary>
public sealed class AcademicStaffListItemDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Email { get; set; }

    /// <summary>docs/MIMARI.md · A-56: ad soyad; boşsa arayüz e-postaya düşer.</summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }
}
