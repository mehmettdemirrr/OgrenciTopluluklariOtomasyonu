namespace Core.Utilities.Security;

/// <summary>
/// docs/MIMARI.md · A-33: audit ve iş kuralları "şu anki kullanıcı"yı bu soyutlama üzerinden öğrenir.
/// WebAPI'de HttpContext'ten, arka plan işlerinde sistem kullanıcısından doldurulur.
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }

    bool IsAuthenticated { get; }
}
