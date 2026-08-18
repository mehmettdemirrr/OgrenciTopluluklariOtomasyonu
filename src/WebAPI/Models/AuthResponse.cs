namespace WebAPI.Models;

/// <summary>docs/MIMARI.md · Y-39: istemciye giden yanıt — refresh token asla burada yer almaz.</summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpiresAtUtc { get; init; }

    public required string CsrfToken { get; init; }
}
