namespace Business.RichText;

/// <summary>
/// docs/MIMARI.md · A-71/A-72/Y-78: zengin metnin KAPALI izin listesi.
/// Buraya bir tip eklemek, arayüzdeki render'a da karşılık eklemeyi gerektirir —
/// sunucunun kabul edip arayüzün çizemediği bir düğüm, boş görünen bir duyuru demektir.
/// </summary>
public static class RichTextSchema
{
    public static readonly IReadOnlySet<string> AllowedNodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "doc", "paragraph", "text", "heading", "bulletList", "orderedList", "listItem", "blockquote", "hardBreak",
    };

    public static readonly IReadOnlySet<string> AllowedMarks = new HashSet<string>(StringComparer.Ordinal)
    {
        "bold", "italic", "underline", "strike", "link", "textColor",
    };

    /// <summary>A-72: renk token'ları; hex DEĞİL. Karşılıkları arayüzdeki temadadır.</summary>
    public static readonly IReadOnlySet<string> AllowedColorTokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "accent", "success", "warning", "danger", "muted",
    };

    public static readonly IReadOnlySet<string> AllowedAlignments = new HashSet<string>(StringComparer.Ordinal)
    {
        "left", "center", "right",
    };

    public static readonly IReadOnlySet<int> AllowedHeadingLevels = new HashSet<int> { 3, 4 };

    /// <summary>Yığın taşmasına ve devasa belgelere karşı sınırlar.</summary>
    public const int MaxDepth = 12;
    public const int MaxNodes = 2_000;
    public const int MaxJsonLength = 200_000;
}
