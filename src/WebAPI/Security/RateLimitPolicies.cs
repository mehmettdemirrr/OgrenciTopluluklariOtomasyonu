namespace WebAPI.Security;

/// <summary>
/// docs/MIMARI.md · A-53/Y-63 (K-13 kısmi): oran sınırı politikalarının adları.
/// Yalnızca **anonim kimlik uçlarında** kullanılır — global limiter yoktur, kimliği doğrulanmış
/// kullanıcı hiç sınırlanmaz.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// E-posta üreten uçlar: <c>register</c>, <c>forgot-password</c>, <c>resend-confirmation</c>.
    /// Y-63'ün asıl hedefi — bu uçlar sınırsız kalırsa SMTP kotası tükenir ve sistemin **tüm**
    /// bildirimleri (kayıt doğrulama dahil) durur.
    /// </summary>
    public const string AuthStrict = "auth-strict";

    /// <summary>
    /// <c>login</c>. Identity'nin hesap kilidi (AuthManager.AccessFailedAsync) hesap başınadır;
    /// bu politika çok sayıda hesaba yayılan deneme (password spray) için IP başına sınır koyar.
    /// </summary>
    public const string AuthLogin = "auth-login";
}
