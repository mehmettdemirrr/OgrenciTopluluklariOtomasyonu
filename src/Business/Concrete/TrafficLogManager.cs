using Business.Abstract;
using Business.DTOs.Traffic;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Dtos.Traffic;

namespace Business.Concrete;

public sealed class TrafficLogManager(ITrafficLogDal trafficLogDal, IClock clock) : ITrafficLogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task RecordAsync(RecordTrafficLogRequestDto request, CancellationToken cancellationToken = default)
    {
        var trafficLog = new TrafficLog
        {
            CorrelationId = request.CorrelationId,
            UserId = request.UserId,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            HttpMethod = request.HttpMethod,
            Path = request.Path,
            RedactedQueryString = request.RedactedQueryString,
            StatusCode = request.StatusCode,
            DurationMs = request.DurationMs,
            TimestampUtc = clock.UtcNow,
        };

        await trafficLogDal.AddAsync(trafficLog, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IDataResult<PagedResult<TrafficLogListItemDto>>> GetPagedAsync(
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
        var result = await trafficLogDal
            .GetPagedAsync(userId, ipAddress, httpMethod, correlationId, fromUtc, toUtc, pageIndex, ClampPageSize(pageSize), cancellationToken)
            .ConfigureAwait(false);

        return DataResult<PagedResult<TrafficLogListItemDto>>.Success(result);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
