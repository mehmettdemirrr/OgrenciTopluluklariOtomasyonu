namespace Business.DTOs.Reference;

public sealed class AcademicTermListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public bool IsCurrent { get; set; }
}
