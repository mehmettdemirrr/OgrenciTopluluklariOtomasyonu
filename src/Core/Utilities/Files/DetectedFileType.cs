namespace Core.Utilities.Files;

/// <summary>docs/MIMARI.md · Y-40: içerik imzasıyla (magic bytes) tespit edilen dosya türü.</summary>
public enum DetectedFileType
{
    Unknown = 0,
    Jpeg,
    Png,
    Webp,

    /// <summary>docs/MIMARI.md · K-37/A-64: kuruluş evrakı. YALNIZCA evrak yolunda kabul edilir.</summary>
    Pdf,
}

public static class DetectedFileTypeExtensions
{
    public static string? ToContentType(this DetectedFileType type) => type switch
    {
        DetectedFileType.Jpeg => "image/jpeg",
        DetectedFileType.Png => "image/png",
        DetectedFileType.Webp => "image/webp",
        DetectedFileType.Pdf => "application/pdf",
        _ => null,
    };

    public static string? ToExtension(this DetectedFileType type) => type switch
    {
        DetectedFileType.Jpeg => ".jpg",
        DetectedFileType.Png => ".png",
        DetectedFileType.Webp => ".webp",
        DetectedFileType.Pdf => ".pdf",
        _ => null,
    };
}
