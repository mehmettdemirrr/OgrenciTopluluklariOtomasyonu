using Core.DataAccess;
using Entities;
using Entities.Dtos.Traffic;

namespace DataAccess.Repositories;

/// <summary>
/// docs/PLAN-V3.md · K-28/A-44: TrafficLog `IEntity` implemente etmediği için generic repository
/// üzerinden erişilemez (bkz. TrafficLog.cs) — IAuditLogDal precedent'iyle özel bir DAL.
/// </summary>
public interface ITrafficLogDal
{
    Task<PagedResult<TrafficLogListItemDto>> GetPagedAsync(
        int? userId,
        string? ipAddress,
        string? httpMethod,
        string? correlationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>A-44: 30 günlük saklama sınırı — gecelik bakım işi tarafından çağrılır.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task AddAsync(TrafficLog trafficLog, CancellationToken cancellationToken = default);
}
