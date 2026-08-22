using Core.DataAccess;
using Entities;
using Entities.Dtos.Audit;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class EfAuditLogDal(AppDbContext context) : IAuditLogDal
{
    public async Task<PagedResult<AuditLogListItemDto>> GetPagedAsync(
        string? entityType,
        string? entityId,
        int? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<AuditLog>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (userId is not null)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (fromUtc is not null)
        {
            query = query.Where(a => a.TimestampUtc >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(a => a.TimestampUtc <= toUtc);
        }

        query = query.OrderByDescending(a => a.TimestampUtc);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogListItemDto
            {
                Id = a.Id,
                UserId = a.UserId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                TimestampUtc = a.TimestampUtc,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<AuditLogListItemDto>(items, totalCount, pageIndex, pageSize);
    }
}
