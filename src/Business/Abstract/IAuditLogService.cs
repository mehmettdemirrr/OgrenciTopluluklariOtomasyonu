using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;
using Entities.Dtos.Audit;

namespace Business.Abstract;

/// <summary>docs/PLAN-V2.md · Faz 13 (K-12): denetim izini görüntüleme — yalnız `audit.read` (Admin).</summary>
public interface IAuditLogService
{
    [SecuredOperation(IdentitySeedData.Permissions.AuditRead)]
    Task<IDataResult<PagedResult<AuditLogListItemDto>>> GetPagedAsync(
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
