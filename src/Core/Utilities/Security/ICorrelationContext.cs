namespace Core.Utilities.Security;

/// <summary>
/// docs/PLAN-V3.md · K-28/A-44: isteğin `TraceIdentifier`'ını taşır — audit satırlarını (AuditLog)
/// aynı isteğin trafik satırına (TrafficLog) bağlamak için tek kaynak. Arka plan işlerinde HTTP
/// bağlamı olmadığı için null döner (ICurrentUser'ın HttpContext-yok davranışıyla aynı ilke).
/// </summary>
public interface ICorrelationContext
{
    string? CorrelationId { get; }
}
