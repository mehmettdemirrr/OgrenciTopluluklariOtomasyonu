namespace Business.DTOs.Auth;

public sealed class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>docs/MIMARI.md · A-56: ad soyad. Zorunlu değil (mevcut hesaplarla tutarlılık için).</summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int EnrollmentYear { get; set; }
}
