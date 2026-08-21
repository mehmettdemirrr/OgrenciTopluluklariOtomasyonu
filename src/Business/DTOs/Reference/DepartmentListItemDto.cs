namespace Business.DTOs.Reference;

public sealed class DepartmentListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int FacultyId { get; set; }
}
