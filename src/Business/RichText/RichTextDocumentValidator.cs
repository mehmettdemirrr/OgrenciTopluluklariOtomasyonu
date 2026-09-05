using System.Text.Json;

namespace Business.RichText;

/// <summary>
/// docs/MIMARI.md · A-71/A-72/Y-78: `ContentJson` yazılmadan önceki tek kapı. Ağacı
/// `RichTextSchema`'nın kapalı izin listesine göre dolaşır; izin listesinde olmayan herhangi
/// bir şey isteği reddeder (fail-closed) — sessizce temizleme yoktur.
/// </summary>
public static class RichTextDocumentValidator
{
    private static readonly IReadOnlySet<string> AttrsParagraph = new HashSet<string>(StringComparer.Ordinal) { "textAlign" };
    private static readonly IReadOnlySet<string> AttrsHeading = new HashSet<string>(StringComparer.Ordinal) { "textAlign", "level" };
    private static readonly IReadOnlySet<string> AttrsNone = new HashSet<string>(StringComparer.Ordinal);

    public static RichTextValidationResult Validate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RichTextValidationResult.Valid();
        }

        if (json.Length > RichTextSchema.MaxJsonLength)
        {
            return RichTextValidationResult.Invalid("İçerik izin verilen boyutu aşıyor.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return RichTextValidationResult.Invalid("İçerik geçerli bir belge değil.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var rootType)
                || rootType.ValueKind != JsonValueKind.String
                || rootType.GetString() != "doc")
            {
                return RichTextValidationResult.Invalid("Kök düğüm 'doc' olmalıdır.");
            }

            var nodeCount = 0;
            return ValidateNode(root, depth: 0, ref nodeCount);
        }
    }

    private static RichTextValidationResult ValidateNode(JsonElement node, int depth, ref int nodeCount)
    {
        if (depth > RichTextSchema.MaxDepth)
        {
            return RichTextValidationResult.Invalid("İçerik iç içe geçme sınırını aşıyor.");
        }

        nodeCount++;
        if (nodeCount > RichTextSchema.MaxNodes)
        {
            return RichTextValidationResult.Invalid("İçerik düğüm sayısı sınırını aşıyor.");
        }

        if (node.ValueKind != JsonValueKind.Object
            || !node.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String)
        {
            return RichTextValidationResult.Invalid("Geçersiz düğüm.");
        }

        var type = typeElement.GetString()!;
        if (!RichTextSchema.AllowedNodes.Contains(type))
        {
            return RichTextValidationResult.Invalid($"Desteklenmeyen düğüm tipi: {type}");
        }

        if (node.TryGetProperty("attrs", out var attrs))
        {
            var attrsResult = ValidateAttrs(type, attrs);
            if (!attrsResult.IsValid)
            {
                return attrsResult;
            }
        }

        if (node.TryGetProperty("marks", out var marks))
        {
            if (marks.ValueKind != JsonValueKind.Array)
            {
                return RichTextValidationResult.Invalid("İşaretler bir dizi olmalıdır.");
            }

            foreach (var mark in marks.EnumerateArray())
            {
                var markResult = ValidateMark(mark);
                if (!markResult.IsValid)
                {
                    return markResult;
                }
            }
        }

        if (type == "text" && (!node.TryGetProperty("text", out var textValue) || textValue.ValueKind != JsonValueKind.String))
        {
            return RichTextValidationResult.Invalid("Metin düğümünde 'text' alanı olmalıdır.");
        }

        if (node.TryGetProperty("content", out var content))
        {
            if (content.ValueKind != JsonValueKind.Array)
            {
                return RichTextValidationResult.Invalid("'content' bir dizi olmalıdır.");
            }

            foreach (var child in content.EnumerateArray())
            {
                var childResult = ValidateNode(child, depth + 1, ref nodeCount);
                if (!childResult.IsValid)
                {
                    return childResult;
                }
            }
        }

        return RichTextValidationResult.Valid();
    }

    private static RichTextValidationResult ValidateAttrs(string nodeType, JsonElement attrs)
    {
        if (attrs.ValueKind != JsonValueKind.Object)
        {
            return RichTextValidationResult.Invalid("'attrs' bir nesne olmalıdır.");
        }

        var allowedKeys = nodeType switch
        {
            "paragraph" => AttrsParagraph,
            "heading" => AttrsHeading,
            _ => AttrsNone,
        };

        foreach (var property in attrs.EnumerateObject())
        {
            if (!allowedKeys.Contains(property.Name))
            {
                return RichTextValidationResult.Invalid($"'{nodeType}' düğümünde desteklenmeyen öznitelik: {property.Name}");
            }

            if (property.Name == "textAlign")
            {
                if (property.Value.ValueKind != JsonValueKind.String || !RichTextSchema.AllowedAlignments.Contains(property.Value.GetString()!))
                {
                    return RichTextValidationResult.Invalid("Geçersiz hizalama.");
                }
            }
            else if (property.Name == "level")
            {
                if (property.Value.ValueKind != JsonValueKind.Number || !RichTextSchema.AllowedHeadingLevels.Contains(property.Value.GetInt32()))
                {
                    return RichTextValidationResult.Invalid("Geçersiz başlık seviyesi.");
                }
            }
        }

        return RichTextValidationResult.Valid();
    }

    private static RichTextValidationResult ValidateMark(JsonElement mark)
    {
        if (mark.ValueKind != JsonValueKind.Object
            || !mark.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String)
        {
            return RichTextValidationResult.Invalid("Geçersiz işaret.");
        }

        var type = typeElement.GetString()!;
        if (!RichTextSchema.AllowedMarks.Contains(type))
        {
            return RichTextValidationResult.Invalid($"Desteklenmeyen işaret tipi: {type}");
        }

        var hasAttrs = mark.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object;
        var attrKeys = hasAttrs ? attrs.EnumerateObject().Select(p => p.Name).ToList() : [];

        if (type == "textColor")
        {
            if (attrKeys is not [(var single1)] || single1 != "token"
                || !attrs.TryGetProperty("token", out var token)
                || token.ValueKind != JsonValueKind.String
                || !RichTextSchema.AllowedColorTokens.Contains(token.GetString()!))
            {
                return RichTextValidationResult.Invalid("Geçersiz renk token'ı.");
            }

            return RichTextValidationResult.Valid();
        }

        if (type == "link")
        {
            if (attrKeys is not [(var single2)] || single2 != "href"
                || !attrs.TryGetProperty("href", out var href)
                || href.ValueKind != JsonValueKind.String
                || !Uri.TryCreate(href.GetString(), UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps)
            {
                return RichTextValidationResult.Invalid("Bağlantı yalnızca https olabilir.");
            }

            return RichTextValidationResult.Valid();
        }

        // bold/italic/underline/strike: öznitelik taşımaz.
        if (hasAttrs && attrKeys.Count > 0)
        {
            return RichTextValidationResult.Invalid($"'{type}' işareti öznitelik taşıyamaz.");
        }

        return RichTextValidationResult.Valid();
    }
}
