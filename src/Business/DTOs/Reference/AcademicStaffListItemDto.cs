namespace Business.DTOs.Reference;

public sealed class AcademicStaffListItemDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Email { get; set; }
}
