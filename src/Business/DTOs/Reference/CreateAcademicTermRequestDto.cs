namespace Business.DTOs.Reference;

public sealed class CreateAcademicTermRequestDto
{
    public string Name { get; set; } = string.Empty;

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }
}
