namespace Business.DTOs.Auth;

public sealed class MeResponseDto
{
    public required string Email { get; set; }

    /// <summary>docs/MIMARI.md · A-56: ad soyad; boşsa arayüz e-postaya düşer.</summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }

    public required IReadOnlyCollection<string> Roles { get; set; }

    public required IReadOnlyCollection<string> Permissions { get; set; }

    /// <summary>
    /// docs/PLAN-V4.md §19.3 (A-48): öğrenci profili alanları. Danışman/admin hesaplarında
    /// `Student` kaydı olmadığı için hepsi <c>null</c> döner.
    /// </summary>
    public string? StudentNumber { get; set; }

    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public string? FacultyName { get; set; }

    public int? EnrollmentYear { get; set; }
}
