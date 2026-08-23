using Core.DataAccess;
using Entities.Dtos.Audit;

namespace DataAccess.Repositories;

/// <summary>
/// docs/PLAN-V2.md · Faz 13 (K-12): AuditLog `IEntity` implemente etmediği için generic repository
/// üzerinden erişilemez (bkz. AuditLog.cs) — okuma tarafı da IReportDal precedent'iyle özel bir DAL.
/// </summary>
public interface IAuditLogDal
{
    Task<PagedResult<AuditLogListItemDto>> GetPagedAsync(
        string? entityType,
        string? entityId,
        int? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? correlationId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}
