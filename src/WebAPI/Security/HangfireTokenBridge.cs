using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebAPI.Security;

/// <summary>
/// docs/MIMARI.md · A-59/Y-65: Hangfire panelinin token köprüsü.
///
/// Panel düz tarayıcı navigasyonuyla açılıyor, yani `Authorization` başlığı gitmiyor. Query
/// string'den token okumak (v4.0'daki hâli) yalnızca <b>ilk</b> isteği çözüyordu: panelin
/// yüklediği CSS/JS varlıkları ve iç gezinmeleri o parametreyi taşımadığı için 401 alıyor,
/// panel de stilsiz açılıyordu.
///
/// Çözüm: query token'la kimlik doğrulandığında yalnızca <c>/hangfire</c> yoluna kapsamlı bir
/// çerez yazılır ve sonraki istekler token'ı oradan okur.
///
/// <b>Y-65 sınırı:</b> çerez <c>Path=/hangfire</c> olduğu için tarayıcı onu <c>/api/*</c>
/// isteklerine <b>göndermez</b> — K-01'in "access token çerezde taşınmaz" kararı ve Y-48'in
/// CSRF savunması yerinde kalır. Köprü yalnızca panelin kendi yolunda geçerlidir.
/// </summary>
public static class HangfireTokenBridge
{
    public const string DashboardPath = "/hangfire";

    // __Host- öneki Path=/ zorunlu kılar, bu çerez ise bilinçli olarak /hangfire kapsamlı (Y-65).
    // __Secure- öneki yalnızca Secure=true ve güvenli origin ister; yol kısıtı getirmez — doğru olan bu.
    private const string CookieName = "__Secure-HangfireAccess";

    /// <summary>Token'ı önce query string'den, yoksa köprü çerezinden okur.</summary>
    public static Task ResolveTokenAsync(MessageReceivedContext context)
    {
        if (!context.Request.Path.StartsWithSegments(DashboardPath))
        {
            return Task.CompletedTask;
        }

        if (context.Request.Query.TryGetValue("access_token", out var queryToken) && !string.IsNullOrEmpty(queryToken))
        {
            context.Token = queryToken;
            return Task.CompletedTask;
        }

        if (context.Request.Cookies.TryGetValue(CookieName, out var cookieToken) && !string.IsNullOrEmpty(cookieToken))
        {
            context.Token = cookieToken;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Query string'le gelen token'ı çereze taşır. Çerez, access token'ın kendi ömrünü aşamaz —
    /// panel erişimi oturumdan uzun yaşamaz.
    /// </summary>
    public static Task PersistTokenAsync(TokenValidatedContext context)
    {
        if (!context.Request.Path.StartsWithSegments(DashboardPath) ||
            !context.Request.Query.TryGetValue("access_token", out var queryToken) ||
            string.IsNullOrEmpty(queryToken))
        {
            return Task.CompletedTask;
        }

        context.Response.Cookies.Append(CookieName, queryToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            // Y-65: kapsam yalnızca panel — tarayıcı bu çerezi /api/* isteklerine göndermez.
            Path = DashboardPath,
            Expires = context.SecurityToken.ValidTo,
        });

        return Task.CompletedTask;
    }

    /// <summary>Çıkışta köprü çerezi de silinir — panel erişimi oturumdan sonra sürmez.</summary>
    public static void ClearCookie(HttpResponse response) =>
        response.Cookies.Delete(CookieName, new CookieOptions { Path = DashboardPath });
}
