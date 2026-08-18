using Business.DTOs.Auth;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// docs/MIMARI.md · K-01/Faz 3: bu üç metot anonim giriş noktalarının kendisidir,
/// [SecuredOperation] taşımazlar.
/// </summary>
public interface IAuthService
{
    Task<IDataResult<IssuedTokensDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<IDataResult<IssuedTokensDto>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<IResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
