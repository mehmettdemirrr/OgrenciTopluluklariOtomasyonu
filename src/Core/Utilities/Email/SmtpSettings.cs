namespace Core.Utilities.Email;

/// <summary>
/// docs/MIMARI.md · Y-20 / docs/PLAN-V3.md · Y-60: Host/Port/EnableSsl/FromName secret değil
/// (appsettings'te durabilir); Username/Password YALNIZCA user-secrets veya ortam değişkeni —
/// bu sınıfta varsayılan değer olarak da yazılamaz (varsayılan, config boşken sessizce devreye girer).
/// </summary>
public sealed class SmtpSettings
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    /// <summary>
    /// 587 (submission) portunda STARTTLS zorunludur — Gmail/Office365 aksi hâlde
    /// "5.7.0 Must issue a STARTTLS command first" ile reddeder. Güvenli tarafta varsayılan true.
    /// </summary>
    public bool EnableSsl { get; init; } = true;

    /// <summary>Boşsa <see cref="Username"/> kullanılır — Gmail zaten From'un kimlik doğrulanan hesapla eşleşmesini ister.</summary>
    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public string? Username { get; init; }

    public string? Password { get; init; }
}
