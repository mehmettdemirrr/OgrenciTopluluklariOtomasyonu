namespace Business.Constants;

/// <summary>
/// docs/MIMARI.md · A-50: liste uçlarının `search` parametresi için tek normalleştirme noktası.
/// Boş/whitespace arama "filtre yok" demektir; uzunluk sınırı A-54'ün cache anahtarını sınırlı
/// tutmasına hizmet eder (serbest metin cache anahtarına giriyor).
/// </summary>
public static class SearchTerm
{
    /// <summary>Cache anahtarına giren serbest metnin üst sınırı (A-54).</summary>
    public const int MaxLength = 100;

    /// <summary>Kırpılmış arama metni; filtre uygulanmayacaksa boş dizi döner.</summary>
    public static string Normalize(string? search)
    {
        var trimmed = search?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        return trimmed.Length > MaxLength ? trimmed[..MaxLength] : trimmed;
    }
}
