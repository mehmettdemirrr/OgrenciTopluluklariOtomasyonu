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

    [Fact(DisplayName = "A-64: %PDF- imzası Pdf olarak tanınır")]
    public void Detect_PdfSignature_ReturnsPdf()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x00, 0x00, 0x00];

        var detected = FileSignatureInspector.Detect(header);

        Assert.Equal(DetectedFileType.Pdf, detected);
        Assert.Equal("application/pdf", detected.ToContentType());
        Assert.Equal(".pdf", detected.ToExtension());
    }

    [Fact(DisplayName = "Y-40: PDF'e benzeyen ama imzası bozuk içerik Unknown döner")]
    public void Detect_BrokenPdfSignature_ReturnsUnknown()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x00, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x00, 0x00, 0x00];

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.Detect(header));
    }

    [Fact(DisplayName = "Y-40: ZIP + word/ + wordprocessingml Docx olarak tanınır")]
    public void DetectContent_DocxZip_ReturnsDocx()
    {
        var content = FakeOfficeBytes("word/document.xml", "wordprocessingml");

        var detected = FileSignatureInspector.DetectContent(content);

        Assert.Equal(DetectedFileType.Docx, detected);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", detected.ToContentType());
        Assert.Equal(".docx", detected.ToExtension());
        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.Detect(content.AsSpan(0, 12)));
    }

    [Fact(DisplayName = "Y-40: xlsx (xl/ + spreadsheetml) Docx sayılmaz")]
    public void DetectContent_XlsxZip_ReturnsUnknown()
    {
        var content = FakeOfficeBytes("xl/workbook.xml", "spreadsheetml");

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(content));
        Assert.False(FileSignatureInspector.LooksLikeDocx(content));
    }

    [Fact(DisplayName = "Y-40: düz ZIP Docx sayılmaz")]
    public void DetectContent_PlainZip_ReturnsUnknown()
    {
        var content = FakeOfficeBytes("readme.txt", "plain-text");

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(content));
    }

    private static byte[] FakeOfficeBytes(string pathMarker, string contentTypeMarker)
    {
        var prefix = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var ascii = System.Text.Encoding.ASCII.GetBytes($"{pathMarker} {contentTypeMarker}");
        return [..prefix, ..ascii];
    }
}
