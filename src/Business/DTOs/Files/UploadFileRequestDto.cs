namespace Business.DTOs.Files;

/// <summary>docs/MIMARI.md · Y-40: IFormFile Business'a sızmaz — WebAPI ham baytları buraya dönüştürür.</summary>
public sealed class UploadFileRequestDto
{
    public required Stream Content { get; set; }

    public required string OriginalFileName { get; set; }

    public long Length { get; set; }
}
