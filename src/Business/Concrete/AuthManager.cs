using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Business.Abstract;
using Business.Constants;
using Business.DTOs.Auth;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Business.Concrete;

/// <summary>
/// docs/MIMARI.md · K-01/K-13/Y-38: SignInManager kullanılmaz (Sdk.Web dışı Business'ta
/// FrameworkReference gerektirir ve cookie akışı için tasarlanmıştır) — kilitleme muhasebesi
/// burada elle yapılır. Refresh rotasyonu tek kullanımlıktır; iptal edilmiş bir token tekrar
/// sunulursa çalıntı/tekrar kullanım sinyali sayılıp kullanıcının tüm aktif token'ları iptal edilir.
/// </summary>
public sealed class AuthManager(
    IIdentityGateway identityGateway,
    IEntityRepository<RefreshToken> refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtSettings,
    IClock clock) : IAuthService
{
    private const string InvalidCredentialsMessage = "E-posta veya parola hatalı.";
    private const string InvalidRefreshTokenMessage = "Oturum geçersiz, lütfen tekrar giriş yapın.";

    public async Task<IDataResult<IssuedTokensDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await identityGateway.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidCredentialsMessage);
        }

        if (await identityGateway.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidCredentialsMessage);
        }

        if (!await identityGateway.CheckPasswordAsync(user, request.Password).ConfigureAwait(false))
        {
            await identityGateway.AccessFailedAsync(user).ConfigureAwait(false);
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidCredentialsMessage);
        }

        // docs/PLAN-V2.md · Faz 11 (K-03): doğrulanmamış e-posta ile giriş reddedilir. Kasıtlı olarak
        // parola kontrolünden SONRA yapılır — yanlış parolayla denenen bir istekten "bu hesap henüz
        // doğrulanmamış" bilgisi sızmaz, yalnızca doğru parolayı bilen (yani hesabın gerçek sahibi
        // olan) kişi bu ayrıntıyı görür.
        if (!user.EmailConfirmed)
        {
            return DataResult<IssuedTokensDto>.Unauthorized(Messages.EmailNotConfirmed);
        }

        await identityGateway.ResetAccessFailedCountAsync(user).ConfigureAwait(false);

        return await IssueTokensAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IDataResult<IssuedTokensDto>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var existing = await refreshTokenRepository.GetAsync(rt => rt.TokenHash == hash, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidRefreshTokenMessage);
        }

        var now = clock.UtcNow;

        if (existing.RevokedAtUtc is not null)
        {
            await RevokeAllActiveTokensAsync(existing.ApplicationUserId, now, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidRefreshTokenMessage);
        }

        if (existing.ExpiresAtUtc <= now)
        {
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidRefreshTokenMessage);
        }

        var user = await identityGateway.FindByIdAsync(existing.ApplicationUserId).ConfigureAwait(false);
        if (user is null)
        {
            return DataResult<IssuedTokensDto>.Unauthorized(InvalidRefreshTokenMessage);
        }

        var (issued, newEntity) = await IssueTokensInternalAsync(user, cancellationToken).ConfigureAwait(false);
        if (!issued.IsSuccess || newEntity is null)
        {
            return issued;
        }

        existing.RevokedAtUtc = now;
        existing.ReplacedByTokenId = newEntity.Id;
        refreshTokenRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return issued;
    }

    public async Task<IResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var existing = await refreshTokenRepository.GetAsync(rt => rt.TokenHash == hash, cancellationToken).ConfigureAwait(false);

        if (existing is null || existing.RevokedAtUtc is not null)
        {
            return Result.Success();
        }

        existing.RevokedAtUtc = clock.UtcNow;
        refreshTokenRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task<IDataResult<IssuedTokensDto>> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        (await IssueTokensInternalAsync(user, cancellationToken).ConfigureAwait(false)).Result;

    /// <summary>
    /// Kaydedilmiş <see cref="RefreshToken"/> satırını da döner ki RefreshAsync, rotasyon zincirini
    /// (ReplacedByTokenId) ekstra bir hash araması yapmadan kurabilsin.
    /// </summary>
    private async Task<(IDataResult<IssuedTokensDto> Result, RefreshToken? Entity)> IssueTokensInternalAsync(
        ApplicationUser user, CancellationToken cancellationToken)
    {
        var permissions = await identityGateway.GetPermissionsAsync(user).ConfigureAwait(false);
        var now = clock.UtcNow;

        var (accessToken, accessTokenExpiresAtUtc) = GenerateAccessToken(user, permissions, now);
        var (rawRefreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, now);

        await refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = new IssuedTokensDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenEntity.ExpiresAtUtc,
        };

        return (DataResult<IssuedTokensDto>.Success(dto), refreshTokenEntity);
    }

    private async Task RevokeAllActiveTokensAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        var activeTokens = await refreshTokenRepository
            .GetListAsync(rt => rt.ApplicationUserId == userId && rt.RevokedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            refreshTokenRepository.Update(token);
        }
    }

    private (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IReadOnlyCollection<string> permissions, DateTime now)
    {
        var settings = jwtSettings.Value;
        var expiresAtUtc = now.AddMinutes(settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
        };
        claims.AddRange(permissions.Select(permission => new Claim(CurrentUserClaimTypes.Permission, permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // notBefore kasıtlı olarak verilmiyor: JwtSecurityToken'ın kurucusu notBefore >= expires
        // olduğunda hata fırlatıyor, bu da testlerde çok kısa/negatif ömürlü token üretimini
        // (bkz. AccessTokenExpiryTests) imkansız kılardı. Y-38'in tek gerçek gereksinimi exp'in
        // doğru işletilmesi; nbf claim'i MIMARI.md tarafından istenmiyor.
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshToken(int userId, DateTime now)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken
        {
            ApplicationUserId = userId,
            TokenHash = Hash(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(jwtSettings.Value.RefreshTokenDays),
        };

        return (rawToken, entity);
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
