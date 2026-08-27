using Business.DTOs.Files;

namespace Business.DTOs.ClubApplications;

/// <summary>
/// docs/MIMARI.md · Y-05/Y-09: Business'ın gördüğü evrak. `IFormFile` BURAYA GİRMEZ —
/// controller stream'i açar, mevcut `UploadFileRequestDto` sözleşmesini kullanır (FilesController deseni).
/// </summary>
public sealed class ClubApplicationDocumentUploadDto
{
    public int DocumentTypeId { get; set; }

    public required UploadFileRequestDto File { get; set; }
}
