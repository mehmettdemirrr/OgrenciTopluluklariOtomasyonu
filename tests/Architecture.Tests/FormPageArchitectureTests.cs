using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-83: form alanları modalda kurulamaz — zengin metin editörü diyaloga girmez.</summary>
public class FormPageArchitectureTests
{
    [Fact(DisplayName = "Y-83: sayfa bileşenlerinde <Dialog> ile RichTextEditor aynı dosyada bulunamaz")]
    public void Pages_DoNotOpenRichTextEditorInsideDialog()
    {
        var pagesDirectory = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src", "pages");
        Assert.True(Directory.Exists(pagesDirectory), $"Sayfa dizini bulunamadı: {pagesDirectory}");

        var offenders = Directory
            .EnumerateFiles(pagesDirectory, "*.tsx", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("<Dialog", StringComparison.Ordinal)
                    && source.Contains("RichTextEditor", StringComparison.Ordinal);
            })
            .Select(path => Path.GetFileName(path))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Y-83 ihlali — form alanları modalda kuruluyor: {string.Join(", ", offenders)}. " +
            "Form sayfası kullan (A-78); modal yalnızca onay sorularına aittir.");
    }
}
