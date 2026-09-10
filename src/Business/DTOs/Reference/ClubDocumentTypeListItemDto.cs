namespace Business.DTOs.Reference;

/// <summary>docs/MIMARI.md · K-37/A-62: kuruluş evrakı tipi kataloğu satırı.</summary>
public sealed class ClubDocumentTypeListItemDto
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public int? TemplateFileId { get; set; }
}
