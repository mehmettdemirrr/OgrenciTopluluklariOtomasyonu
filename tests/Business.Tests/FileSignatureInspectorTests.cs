using System.IO.Compression;
using System.Text;
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

    [Fact(DisplayName = "A-83: Word'ün ürettiği gerçek .docx paketi Docx olarak tanınır")]
    public void DetectContent_RealDocxPackage_ReturnsDocx()
    {
        var content = BuildOfficePackage("word/document.xml", "wordprocessingml");

        // Hatanın kendisi: işaret SIKIŞTIRILMIŞ girdinin içindedir, ham baytlarda yoktur.
        // Bu satır düşerse test artık hatayı üretmiyordur — dize taraması yeniden geçer hâle gelir.
        Assert.DoesNotContain("wordprocessingml", Encoding.ASCII.GetString(content), StringComparison.Ordinal);

        var detected = FileSignatureInspector.DetectContent(content);

        Assert.Equal(DetectedFileType.Docx, detected);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", detected.ToContentType());
        Assert.Equal(".docx", detected.ToExtension());
    }

    [Fact(DisplayName = "Y-88: xlsx (xl/ + spreadsheetml) Docx sayılmaz")]
    public void DetectContent_XlsxPackage_ReturnsUnknown()
    {
        var content = BuildOfficePackage("xl/workbook.xml", "spreadsheetml");

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(content));
    }

    [Fact(DisplayName = "Y-88: düz ZIP Docx sayılmaz")]
    public void DetectContent_PlainZip_ReturnsUnknown()
    {
        var content = BuildOfficePackage("readme.txt", "plain-text");

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(content));
    }

    [Fact(DisplayName = "Y-88: bozuk/kesik ZIP çökmez, Unknown döner")]
    public void DetectContent_TruncatedZip_ReturnsUnknown()
    {
        var content = BuildOfficePackage("word/document.xml", "wordprocessingml");
        var truncated = content.AsSpan(0, content.Length / 2).ToArray();

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(truncated));
    }

    [Fact(DisplayName = "A-83: depodaki gerçek MTÜ şablonu (FR-0239.docx) kabul edilir")]
    public void DetectContent_RepositoryTemplate_ReturnsDocx()
    {
        var path = Path.Combine(FindRepositoryRoot(), "arayuz", "public", "club-document-templates", "FR-0239.docx");
        Assert.True(File.Exists(path), $"Şablon bulunamadı: {path}");

        Assert.Equal(DetectedFileType.Docx, FileSignatureInspector.DetectContent(File.ReadAllBytes(path)));
    }

    /// <summary>
    /// Gerçek bir OOXML paketi üretir: girdi ADLARI sıkıştırılmadan, girdi İÇERİKLERİ deflate ile
    /// yazılır — Word'ün ürettiği dosyanın davranışı. Sahte (düz ASCII) tampon, A-83'teki hatayı gizler.
    /// </summary>
    private static byte[] BuildOfficePackage(string entryPath, string contentTypeMarker)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contentTypes = archive.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(contentTypes.Open()))
            {
                // Deflate'in gerçekten sıkıştırma seçmesi için tekrarlı ve yeterince uzun içerik.
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types>");
                for (var i = 0; i < 40; i++)
                {
                    writer.Write($"<Default Extension=\"rels{i}\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                }

                writer.Write($"<Override PartName=\"/{entryPath}\" ContentType=\"application/vnd.openxmlformats-officedocument.{contentTypeMarker}.document.main+xml\"/></Types>");
            }

            var document = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var documentWriter = new StreamWriter(document.Open());
            documentWriter.Write("<w:document><w:body/></w:document>");
        }

        return buffer.ToArray();
    }

    /// <summary>Architecture.Tests'teki SolutionPaths ile aynı yöntem: *.slnx dosyasına kadar yukarı yürü.</summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles("*.slnx").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Çözüm kökü (*.slnx) bulunamadı: " + AppContext.BaseDirectory);
    }
}
