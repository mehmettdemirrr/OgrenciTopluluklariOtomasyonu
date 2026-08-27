namespace Business.DTOs.Reference;

public sealed class CreateClubDocumentTypeRequestDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }
}
