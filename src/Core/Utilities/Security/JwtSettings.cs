namespace Core.Utilities.Security;

/// <summary>
/// docs/MIMARI.md · K-01: access 15 dk, refresh 7 gün. Y-20: Key appsettings'te tutulmaz,
/// user-secrets/ortam değişkeninden bağlanır.
/// </summary>
public sealed class JwtSettings
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string Key { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;
}
