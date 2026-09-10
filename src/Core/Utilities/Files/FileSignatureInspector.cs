using System.IO.Compression;

namespace Core.Utilities.Files;

/// <summary>
/// docs/MIMARI.md · Y-40: dosya tipi uzantıya değil, ilk baytlardaki imzaya (magic bytes) bakılarak
/// belirlenir — istemcinin verdiği ContentType/dosya adı asla güvenilmez.
/// </summary>
public static class FileSignatureInspector
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // "%PDF-" — PDF dosyaları daima bu beş baytla başlar (ISO 32000-1 §7.5.2).
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46, 0x2D];

    public static DetectedFileType Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= JpegSignature.Length && header[..JpegSignature.Length].SequenceEqual(JpegSignature))
        {
            return DetectedFileType.Jpeg;
        }

        if (header.Length >= PngSignature.Length && header[..PngSignature.Length].SequenceEqual(PngSignature))
        {
            return DetectedFileType.Png;
        }

        // WebP: baytlar 0-3 "RIFF", baytlar 8-11 "WEBP" (RIFF konteynerinin ortasında format etiketi).
        if (header.Length >= 12 &&
            header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
        {
            return DetectedFileType.Webp;
        }

        if (header.Length >= PdfSignature.Length && header[..PdfSignature.Length].SequenceEqual(PdfSignature))
        {
            return DetectedFileType.Pdf;
        }

        return DetectedFileType.Unknown;
    }

    /// <summary>
    /// docs/MIMARI.md · A-83: header imzası, sonra OOXML paket doğrulaması. ZIP sihirli baytı tek
    /// başına Docx sayılmaz; paket açılıp girdi adı ve içerik tipi birlikte doğrulanır (Y-88).
    /// </summary>
    public static DetectedFileType DetectContent(byte[] content)
    {
        var headerLength = Math.Min(12, content.Length);
        var detected = Detect(content.AsSpan(0, headerLength));
        if (detected != DetectedFileType.Unknown)
        {
            return detected;
        }

        return IsZipLocalFileHeader(content) && IsWordPackage(content)
            ? DetectedFileType.Docx
            : DetectedFileType.Unknown;
    }

    public static bool IsZipLocalFileHeader(ReadOnlySpan<byte> content) =>
        content.Length >= 4 &&
        content[0] == 0x50 && content[1] == 0x4B && content[2] == 0x03 && content[3] == 0x04;

    /// <summary>Untrusted paket: yalnızca girdi adları listelenir, tek küçük girdi tavanla okunur.</summary>
    private const int MaxContentTypesBytes = 64 * 1024;

    private static bool IsWordPackage(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.GetEntry("word/document.xml") is null)
            {
                return false;
            }

            var contentTypes = archive.GetEntry("[Content_Types].xml");
            if (contentTypes is null || contentTypes.Length > MaxContentTypesBytes)
            {
                return false;
            }

            using var entryStream = contentTypes.Open();
            using var reader = new StreamReader(entryStream);
            return reader.ReadToEnd().Contains("wordprocessingml", StringComparison.Ordinal);
        }
        catch (InvalidDataException)
        {
            // Bozuk/kesik arşiv: tip doğrulanamadı → Unknown (çağıran reddeder).
            return false;
        }
    }
}
