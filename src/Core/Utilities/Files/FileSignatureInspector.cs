namespace Core.Utilities.Files;

/// <summary>
/// docs/MIMARI.md · Y-40: dosya tipi uzantıya değil, ilk baytlardaki imzaya (magic bytes) bakılarak
/// belirlenir — istemcinin verdiği ContentType/dosya adı asla güvenilmez.
/// </summary>
public static class FileSignatureInspector
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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

        return DetectedFileType.Unknown;
    }
}
