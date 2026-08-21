namespace Entities.Dtos.Reference;

public sealed class AcademicStaffRowDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Email { get; set; }
}
