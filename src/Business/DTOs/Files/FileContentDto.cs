namespace Business.DTOs.Files;

public sealed class FileContentDto
{
    public required Stream Content { get; set; }

    public required string ContentType { get; set; }

    public required string DownloadFileName { get; set; }
}
