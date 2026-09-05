using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Business.RichText;

/// <summary>
/// docs/MIMARI.md · A-71: `Content`'in düz metin aynasını `ContentJson`'dan türetir —
/// arama (`GetFeedAsync`), e-posta ve vitrin önizlemesi bu alandan okumaya devam eder.
/// </summary>
public static partial class RichTextPlainTextExtractor
{
    private static readonly HashSet<string> BlockNodes = new(StringComparer.Ordinal)
    {
        "paragraph", "heading", "listItem", "blockquote",
    };

    public static string Extract(string json)
    {
        using var document = JsonDocument.Parse(json);
        var builder = new StringBuilder();
        AppendNode(document.RootElement, builder);

        return CollapseWhitespace(builder.ToString());
    }

    private static void AppendNode(JsonElement node, StringBuilder builder)
    {
        if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        var type = typeElement.GetString();

        if (type == "text" && node.TryGetProperty("text", out var textValue) && textValue.ValueKind == JsonValueKind.String)
        {
            builder.Append(textValue.GetString());
        }

        if (type == "hardBreak")
        {
            builder.Append('\n');
        }

        if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                AppendNode(child, builder);
            }
        }

        if (type is not null && BlockNodes.Contains(type))
        {
            builder.Append('\n');
        }
    }

    private static string CollapseWhitespace(string text)
    {
        var lines = text.Split('\n')
            .Select(line => InlineWhitespace().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);

        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex InlineWhitespace();
}
