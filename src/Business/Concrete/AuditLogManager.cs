using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Repositories;
using Entities.Dtos.Audit;

namespace Business.Concrete;

public sealed class AuditLogManager(IAuditLogDal auditLogDal) : IAuditLogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<AuditLogListItemDto>>> GetPagedAsync(
        string? entityType,
        string? entityId,
        int? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await auditLogDal
            .GetPagedAsync(entityType, entityId, userId, fromUtc, toUtc, pageIndex, ClampPageSize(pageSize), cancellationToken)
            .ConfigureAwait(false);

        return DataResult<PagedResult<AuditLogListItemDto>>.Success(result);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
