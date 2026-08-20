namespace Business.DTOs.Files;

public sealed class UploadedFileDto
{
    public int FileId { get; set; }

    public required string ContentType { get; set; }

    public long FileSizeBytes { get; set; }
}
