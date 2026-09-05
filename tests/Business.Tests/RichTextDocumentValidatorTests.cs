using Business.RichText;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-71/A-72/Y-78: zengin metnin kapalı izin listesi.</summary>
public class RichTextDocumentValidatorTests
{
    private const string ValidDoc = """
    {"type":"doc","content":[
      {"type":"paragraph","attrs":{"textAlign":"left"},"content":[
        {"type":"text","text":"Kayıtlar ","marks":[{"type":"bold"}]},
        {"type":"text","text":"15 Ekim","marks":[{"type":"textColor","attrs":{"token":"accent"}}]}
      ]}
    ]}
    """;

    [Fact]
    public void Validate_AcceptsAllowedNodesMarksAndColorTokens()
    {
        var result = RichTextDocumentValidator.Validate(ValidDoc);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsUnknownNodeType()
    {
        var doc = """{"type":"doc","content":[{"type":"iframe","attrs":{"src":"https://evil.example"}}]}""";

        var result = RichTextDocumentValidator.Validate(doc);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsUnknownMarkType()
    {
        var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"script"}]}]}]}""";

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_RejectsRawHexColor()
    {
        // A-72: renk token'dır; hex kabul edilmez (koyu modda okunmaz hale gelir).
        var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"textColor","attrs":{"token":"#ff0000"}}]}]}]}""";

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_RejectsNonHttpsLink()
    {
        var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"link","attrs":{"href":"javascript:alert(1)"}}]}]}]}""";

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_RejectsUnknownAttrOnLinkMark()
    {
        // target/rel dahil href dışında öznitelik kabul edilmez.
        var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"link","attrs":{"href":"https://ozal.edu.tr","target":"_blank"}}]}]}]}""";

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_AcceptsHttpsLink()
    {
        var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"link","attrs":{"href":"https://ozal.edu.tr"}}]}]}]}""";

        Assert.True(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_RejectsDocumentDeeperThanLimit()
    {
        // Özyinelemeli gezinme yığın taşmasına sürüklenemez.
        var doc = BuildNestedBulletLists(depth: 40);

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_RejectsMalformedJson()
    {
        Assert.False(RichTextDocumentValidator.Validate("{ not json").IsValid);
    }

    [Fact]
    public void Validate_RejectsRootNotDoc()
    {
        Assert.False(RichTextDocumentValidator.Validate("""{"type":"paragraph"}""").IsValid);
    }

    [Fact]
    public void Validate_RejectsUnknownAttrKey()
    {
        var doc = """{"type":"doc","content":[{"type":"paragraph","attrs":{"class":"evil"},"content":[]}]}""";

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_RejectsUnknownHeadingLevel()
    {
        var doc = """{"type":"doc","content":[{"type":"heading","attrs":{"level":1},"content":[]}]}""";

        Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
    }

    [Fact]
    public void Validate_AcceptsNullOrEmpty()
    {
        Assert.True(RichTextDocumentValidator.Validate(null).IsValid);
        Assert.True(RichTextDocumentValidator.Validate("").IsValid);
    }

    [Fact]
    public void Extract_ProducesPlainTextMirror()
    {
        var text = RichTextPlainTextExtractor.Extract(ValidDoc);

        Assert.Equal("Kayıtlar 15 Ekim", text);
    }

    [Fact]
    public void Extract_SeparatesBlockNodesWithNewline()
    {
        var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Birinci"}]},{"type":"paragraph","content":[{"type":"text","text":"İkinci"}]}]}""";

        var text = RichTextPlainTextExtractor.Extract(doc);

        Assert.Equal("Birinci\nİkinci", text);
    }

    private static string BuildNestedBulletLists(int depth)
    {
        var json = """{"type":"text","text":"x"}""";
        for (var i = 0; i < depth; i++)
        {
            json = $$"""{"type":"bulletList","content":[{"type":"listItem","content":[{{json}}]}]}""";
        }

        return $$"""{"type":"doc","content":[{{json}}]}""";
    }
}
