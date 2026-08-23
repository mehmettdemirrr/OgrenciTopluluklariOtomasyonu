using Core.Utilities.Security;

namespace WebAPI.Security;

/// <summary>docs/PLAN-V3.md · K-28: ICorrelationContext'in istek bağlamındaki implementasyonu (HttpContextCurrentUser kalıbı).</summary>
public sealed class HttpContextCorrelationContext(IHttpContextAccessor httpContextAccessor) : ICorrelationContext
{
    public string? CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier;
}
