using Core.DataAccess;
using Entities;
using Entities.Dtos.Traffic;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class EfTrafficLogDal(AppDbContext context) : ITrafficLogDal
{
    public async Task<PagedResult<TrafficLogListItemDto>> GetPagedAsync(
        int? userId,
        string? ipAddress,
        string? httpMethod,
        string? correlationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<TrafficLog>().AsNoTracking().AsQueryable();

        if (userId is not null)
        {
            query = query.Where(t => t.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            query = query.Where(t => t.IpAddress == ipAddress);
        }

        if (!string.IsNullOrWhiteSpace(httpMethod))
        {
            query = query.Where(t => t.HttpMethod == httpMethod);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(t => t.CorrelationId == correlationId);
        }

        if (fromUtc is not null)
        {
            query = query.Where(t => t.TimestampUtc >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(t => t.TimestampUtc <= toUtc);
        }

        query = query.OrderByDescending(t => t.TimestampUtc);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(t => new TrafficLogListItemDto
            {
                Id = t.Id,
                CorrelationId = t.CorrelationId,
                UserId = t.UserId,
                IpAddress = t.IpAddress,
                UserAgent = t.UserAgent,
                HttpMethod = t.HttpMethod,
                Path = t.Path,
                RedactedQueryString = t.RedactedQueryString,
                StatusCode = t.StatusCode,
                DurationMs = t.DurationMs,
                TimestampUtc = t.TimestampUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TrafficLogListItemDto>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        await context.Set<TrafficLog>()
            .Where(t => t.TimestampUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(TrafficLog trafficLog, CancellationToken cancellationToken = default)
    {
        await context.Set<TrafficLog>().AddAsync(trafficLog, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
