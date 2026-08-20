using Core.Utilities.Files;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-40: içerik imzası (magic bytes) uzantı/istemci ContentType'ından bağımsız çalışmalı.</summary>
public class FileSignatureInspectorTests
{
    [Fact(DisplayName = "Detect: JPEG imzası (FF D8 FF) tanınır")]
    public void Detect_JpegSignature_ReturnsJpeg()
    {
        byte[] header = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        Assert.Equal(DetectedFileType.Jpeg, FileSignatureInspector.Detect(header));
    }

    [Fact(DisplayName = "Detect: PNG imzası tanınır")]
    public void Detect_PngSignature_ReturnsPng()
    {
        byte[] header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        Assert.Equal(DetectedFileType.Png, FileSignatureInspector.Detect(header));
    }

    [Fact(DisplayName = "Detect: WebP imzası (RIFF....WEBP) tanınır")]
    public void Detect_WebpSignature_ReturnsWebp()
    {
        byte[] header = "RIFF\0\0\0\0WEBP"u8.ToArray();

        Assert.Equal(DetectedFileType.Webp, FileSignatureInspector.Detect(header));
    }

    [Fact(DisplayName = "Detect: tanınmayan/sahte imza (uzantı .png olsa da) Unknown döner")]
    public void Detect_TextContentDisguisedAsPng_ReturnsUnknown()
    {
        byte[] header = "bu aslinda metin"u8.ToArray();

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.Detect(header));
    }

    [Fact(DisplayName = "Detect: çok kısa header Unknown döner (dizi taşması yok)")]
    public void Detect_TooShortHeader_ReturnsUnknown()
    {
        byte[] header = [0xFF];

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.Detect(header));
    }
}
