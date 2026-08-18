using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-05/A-31/A-36/Y-52: yüklenen dosya meta verisi.
/// Görünürlük yükleme anında belirlenir: logo/afiş Public, rapor çıktıları Protected.
/// </summary>
public sealed class StoredFile : IEntity
{
    public int Id { get; set; }

    public required string GeneratedFileName { get; set; }

    public required string OriginalFileName { get; set; }

    public required string ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public FileVisibility Visibility { get; set; }

    public int UploadedByUserId { get; set; }

    public DateTime UploadedAtUtc { get; set; }
}
