namespace Business.RichText;

/// <summary>docs/MIMARI.md · Y-78: fail-closed — geçersiz içerik sessizce temizlenmez, sebep döner.</summary>
public readonly record struct RichTextValidationResult(bool IsValid, string? Error)
{
    public static RichTextValidationResult Valid() => new(true, null);

    public static RichTextValidationResult Invalid(string error) => new(false, error);
}
