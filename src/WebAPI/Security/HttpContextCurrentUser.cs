using System.Security.Claims;
using Core.Utilities.Security;

namespace WebAPI.Security;

/// <summary>docs/MIMARI.md · A-33: ICurrentUser'ın istek bağlamındaki implementasyonu.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
