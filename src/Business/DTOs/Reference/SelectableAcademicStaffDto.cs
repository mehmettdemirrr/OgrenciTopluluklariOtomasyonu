namespace Business.DTOs.Reference;

/// <summary>
/// docs/PLAN-V3.md §17.4: AcademicStaffListItemDto'dan bilinçli olarak ayrı — reference.manage
/// yerine yalnızca kimlik doğrulaması ister (öğrenci danışman seçebilsin diye). Ayrı DTO ailesi,
/// admin ucuna sonradan eklenecek bir alanın buradan sessizce sızmasını yapısal olarak engeller.
/// </summary>
public sealed class SelectableAcademicStaffDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Email { get; set; }
}
