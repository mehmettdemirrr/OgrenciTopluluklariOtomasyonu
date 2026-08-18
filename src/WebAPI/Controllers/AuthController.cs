using Business.Abstract;
using Business.DTOs.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;
using WebAPI.Filters;
using WebAPI.Models;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-01/Y-39/Y-48: refresh token yalnızca httpOnly çerezde taşınır, asla JSON gövdede değil.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IAntiforgery antiforgery) : ControllerBase
{
    private const string RefreshTokenCookieName = "RefreshToken";

    [HttpPost("login")]
    [AllowAnonymous]
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
