namespace Business.DTOs.ClubApplications;

/// <summary>
/// docs/MIMARI.md · K-37: inceleme ekranının gördüğü evrak. Dosyanın kendisi burada YOK —
/// yalnızca korumalı indirme ucundan alınır (A-63/Y-70). `StoredFileId` de taşınmaz:
/// istemcinin bilmesi gereken tek kimlik `DocumentId`.
/// </summary>
public sealed class ClubApplicationDocumentDto
{
    public int DocumentId { get; set; }

    public int DocumentTypeId { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public bool IsRequired { get; set; }

    public required string OriginalFileName { get; set; }

    public long FileSizeBytes { get; set; }
}
