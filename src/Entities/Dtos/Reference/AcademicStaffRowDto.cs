namespace Entities.Dtos.Reference;

public sealed class AcademicStaffRowDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Email { get; set; }

    /// <summary>docs/MIMARI.md · A-56: ad soyad; boşsa çağıran e-postaya düşer.</summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }
}
