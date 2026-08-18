namespace Business.DTOs.Auth;

/// <summary>
/// docs/MIMARI.md · Y-39: yalnızca Business→WebAPI sınırı için — controller RefreshToken'ı
/// httpOnly çereze yazar, asla JSON gövdede istemciye döndürmez.
/// </summary>
public sealed class IssuedTokensDto
{
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpiresAtUtc { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime RefreshTokenExpiresAtUtc { get; init; }
}
