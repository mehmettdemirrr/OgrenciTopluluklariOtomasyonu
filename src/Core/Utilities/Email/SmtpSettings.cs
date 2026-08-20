namespace Core.Utilities.Email;

/// <summary>
/// docs/MIMARI.md · Y-20: Host/Port/FromAddress/FromName secret değil (appsettings'te durabilir);
/// Username/Password varsa yalnızca user-secrets/ortam değişkeni.
/// </summary>
public sealed class SmtpSettings
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public string? Username { get; init; }

    public string? Password { get; init; }
}
