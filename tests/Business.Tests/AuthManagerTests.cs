using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.DTOs.Auth;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20: her iş kuralı için bir kabul + bir ret.</summary>
public class AuthManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IIdentityGateway> _identityGateway = new();
    private readonly Mock<IEntityRepository<RefreshToken>> _refreshTokenRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AuthManager _sut;

    public AuthManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        var jwtSettings = Options.Create(new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "test-signing-key-please-replace-32-bytes+",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
        });

        _sut = new AuthManager(
            _identityGateway.Object,
            _refreshTokenRepository.Object,
            _unitOfWork.Object,
            jwtSettings,
            _clock.Object);
    }

    private static ApplicationUser CreateUser(int id = 1) =>
        new() { Id = id, Email = "user@test.local", UserName = "user@test.local" };

    [Fact(DisplayName = "Login: yanlış parola reddedilir ve AccessFailedAsync çağrılır")]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        var user = CreateUser();
        _identityGateway.Setup(g => g.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _identityGateway.Setup(g => g.IsLockedOutAsync(user)).ReturnsAsync(false);
        _identityGateway.Setup(g => g.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var result = await _sut.LoginAsync(new LoginRequestDto { Email = user.Email!, Password = "wrong" });

        Assert.False(result.IsSuccess);
        _identityGateway.Verify(g => g.AccessFailedAsync(user), Times.Once);
    }

    [Fact(DisplayName = "Login: doğru kimlik bilgileriyle token üretilir ve kaydedilir")]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
    {
        var user = CreateUser();
        _identityGateway.Setup(g => g.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _identityGateway.Setup(g => g.IsLockedOutAsync(user)).ReturnsAsync(false);
        _identityGateway.Setup(g => g.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        _identityGateway.Setup(g => g.GetPermissionsAsync(user)).ReturnsAsync(new[] { "clubs.read" });

        var result = await _sut.LoginAsync(new LoginRequestDto { Email = user.Email!, Password = "correct" });

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        _identityGateway.Verify(g => g.ResetAccessFailedCountAsync(user), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "Login: kilitli hesap reddedilir, parola hiç kontrol edilmez")]
    public async Task LoginAsync_LockedOutAccount_ReturnsUnauthorized()
    {
        var user = CreateUser();
        _identityGateway.Setup(g => g.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _identityGateway.Setup(g => g.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await _sut.LoginAsync(new LoginRequestDto { Email = user.Email!, Password = "anything" });

        Assert.False(result.IsSuccess);
        _identityGateway.Verify(g => g.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Refresh: süresi dolmuş token reddedilir")]
    public async Task RefreshAsync_ExpiredToken_ReturnsUnauthorized()
    {
        var expired = new RefreshToken
        {
            Id = 1,
            ApplicationUserId = 1,
            TokenHash = "hash",
            CreatedAtUtc = FixedNow.AddDays(-8),
            ExpiresAtUtc = FixedNow.AddDays(-1),
        };
        _refreshTokenRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        var result = await _sut.RefreshAsync("raw-token");

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "Refresh: iptal edilmiş (tekrar kullanılmış) token reddedilir ve kullanıcının tüm aktif token'ları iptal edilir (Y-38)")]
    public async Task RefreshAsync_RevokedToken_ReturnsUnauthorizedAndRevokesAllActiveTokens()
    {
        var revoked = new RefreshToken
        {
            Id = 1,
            ApplicationUserId = 1,
            TokenHash = "hash",
            CreatedAtUtc = FixedNow.AddDays(-1),
            ExpiresAtUtc = FixedNow.AddDays(6),
            RevokedAtUtc = FixedNow.AddMinutes(-5),
        };
        _refreshTokenRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revoked);
        _refreshTokenRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([revoked]);

        var result = await _sut.RefreshAsync("raw-token");

        Assert.False(result.IsSuccess);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "Refresh: geçerli token rotate edilir, eski satır iptal edilip yeni satıra bağlanır")]
    public async Task RefreshAsync_ValidToken_RotatesAndReturnsSuccess()
    {
        var user = CreateUser();
        var existing = new RefreshToken
        {
            Id = 1,
            ApplicationUserId = user.Id,
            TokenHash = "hash",
            CreatedAtUtc = FixedNow.AddDays(-1),
            ExpiresAtUtc = FixedNow.AddDays(6),
        };

        _refreshTokenRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _identityGateway.Setup(g => g.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _identityGateway.Setup(g => g.GetPermissionsAsync(user)).ReturnsAsync(Array.Empty<string>());

        // AddAsync sırasında EF'in normalde doldurduğu Id'yi burada elle simüle ediyoruz.
        _refreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((entity, _) => entity.Id = 2)
            .Returns(Task.CompletedTask);

        var result = await _sut.RefreshAsync("raw-token");

        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, existing.RevokedAtUtc!.Value);
        Assert.Equal(2, existing.ReplacedByTokenId);
        _refreshTokenRepository.Verify(r => r.Update(existing), Times.AtLeastOnce);
    }
}
