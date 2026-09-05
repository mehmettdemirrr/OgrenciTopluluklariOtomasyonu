using System.Text.RegularExpressions;
using Business.RichText;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-78: zengin içerik HTML olarak basılamaz; renk token'ları sunucu/arayüz
/// arasında ayrışamaz. .NET tarafında karşılığı olmayan bu kuralların tek bekçisi kaynak taramasıdır.
/// </summary>
public class RichTextSafetyTests
{
    [Fact]
    public void Frontend_NeverInjectsHtml()
    {
        var root = SolutionPaths.FindRepositoryRoot();
        var offenders = Directory
            .EnumerateFiles(Path.Combine(root, "arayuz", "src"), "*.ts*", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("dangerouslySetInnerHTML", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.True(offenders.Count == 0, $"Y-78 ihlali: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void ColorTokens_MatchBetweenServerAndFrontend()
    {
        var root = SolutionPaths.FindRepositoryRoot();
        var frontendTokens = ExtractRichTextColorTokens(Path.Combine(root, "arayuz", "src", "theme", "tokens.ts"));

        Assert.Equal(
            RichTextSchema.AllowedColorTokens.OrderBy(x => x, StringComparer.Ordinal),
            frontendTokens.OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// `richTextColors.light` bloğundaki anahtar adlarını okur (değerleri değil — hex'ler
    /// A-72 gereği yalnızca burada yaşar, doğrulanacak olan isim kümesidir).
    /// </summary>
    private static List<string> ExtractRichTextColorTokens(string tokensFilePath)
    {
        var content = File.ReadAllText(tokensFilePath);
        var lightBlockStart = content.IndexOf("richTextColors", StringComparison.Ordinal);
        if (lightBlockStart < 0)
        {
            throw new InvalidOperationException("tokens.ts içinde 'richTextColors' bulunamadı.");
        }

        var lightKeywordStart = content.IndexOf("light:", lightBlockStart, StringComparison.Ordinal);
        var lightBraceStart = content.IndexOf('{', lightKeywordStart) + 1;
        var darkStart = content.IndexOf("dark:", lightBraceStart, StringComparison.Ordinal);
        var lightBlock = content[lightBraceStart..darkStart];

        // Satır başındaki "  key: value," biçimindeki anahtarları topla.
        var matches = Regex.Matches(lightBlock, @"^\s*([a-zA-Z]+):", RegexOptions.Multiline);
        return matches.Select(m => m.Groups[1].Value).ToList();
    }
}
