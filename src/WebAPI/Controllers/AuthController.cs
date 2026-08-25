using Business.Abstract;
using Business.DTOs.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Extensions;
using WebAPI.Filters;
using WebAPI.Models;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-01/Y-39/Y-48: refresh token yalnızca httpOnly çerezde taşınır, asla JSON gövdede değil.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IAccountService accountService, IAntiforgery antiforgery) : ControllerBase
{
    private const string RefreshTokenCookieName = "RefreshToken";

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.RegisterAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("departments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRegistrationDepartments(CancellationToken cancellationToken)
    {
        var result = await accountService.GetRegistrationDepartmentsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.ConfirmEmailAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.ResendConfirmationAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.ForgotPasswordAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.ResetPasswordAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.ChangePasswordAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        SetRefreshTokenCookie(result.Data);
        return Ok(BuildAuthResponse(result.Data));
    }

    /// <summary>
    /// docs/MIMARI.md · A-59: sayfa yenilendiğinde bellekteki CSRF istek token'ı da kaybolur —
    /// bu uç olmadan açılıştaki sessiz refresh, `X-XSRF-TOKEN` başlığını üretemediği için
    /// daha ilk adımda başarısız olur (K-01'in oturum sürekliliği bu yüzden hiç çalışmıyordu).
    ///
    /// Y-48 zayıflamaz: `GetAndStoreTokens` çerez token'ını çağıranın KENDİ tarayıcısına yazar ve
    /// ona karşılık gelen istek token'ını döner. Başka origin'den çağıran saldırgan kurbanın
    /// çerezini okuyamaz, kendi aldığı çift ise kurbanın oturumunda işe yaramaz.
    /// </summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { csrfToken = tokens.RequestToken! });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [TypeFilter(typeof(AntiforgeryActionFilter))]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (!result.IsSuccess)
        {
            Response.Cookies.Delete(RefreshTokenCookieName);
            return result.ToActionResult();
        }

        SetRefreshTokenCookie(result.Data);
        return Ok(BuildAuthResponse(result.Data));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await authService.LogoutAsync(refreshToken, cancellationToken);
        }

        Response.Cookies.Delete(RefreshTokenCookieName);

        // Y-65: panel erişimi oturumdan sonra sürmesin.
        HangfireTokenBridge.ClearCookie(Response);
        return Ok();
    }

    private void SetRefreshTokenCookie(IssuedTokensDto tokens) =>
        Response.Cookies.Append(RefreshTokenCookieName, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = tokens.RefreshTokenExpiresAtUtc,
        });

    private AuthResponse BuildAuthResponse(IssuedTokensDto tokens)
    {
        var csrfTokens = antiforgery.GetAndStoreTokens(HttpContext);

        return new AuthResponse
        {
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            CsrfToken = csrfTokens.RequestToken!,
        };
    }
}
