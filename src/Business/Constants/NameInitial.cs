using System.Globalization;

namespace Business.Constants;

/// <summary>
/// A-50/Y-62: vitrin alfabetik filtresi. Tek harf cache anahtarına girer; geçersiz değer
/// "filtre yok" sayılır — IsActive sabit filtresini gevşetmez.
/// </summary>
public static class NameInitial
{
    public const string Alphabet = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";

    public static string Normalize(string? letter)
    {
        if (string.IsNullOrWhiteSpace(letter))
        {
            return string.Empty;
        }

        var upper = letter.Trim().ToUpper(new CultureInfo("tr-TR"));
        if (upper.Length != 1 || Alphabet.IndexOf(upper[0]) < 0)
        {
            return string.Empty;
        }

        return upper;
    }

    public static string ToSearchLower(string initial) =>
        initial.Length == 0 ? string.Empty : initial.ToLower(new CultureInfo("tr-TR"));
}
