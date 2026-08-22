namespace Business.DTOs.Reference;

public sealed class UpdateAcademicTermRequestDto
{
    public string Name { get; set; } = string.Empty;

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }
}
