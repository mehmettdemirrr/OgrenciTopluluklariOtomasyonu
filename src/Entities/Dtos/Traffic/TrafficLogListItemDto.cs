namespace Entities.Dtos.Traffic;

/// <summary>docs/PLAN-V3.md · K-28/A-44: RequestLoggingMiddleware'in yazdığı satırın okuma projeksiyonu.</summary>
public sealed class TrafficLogListItemDto
{
    public long Id { get; set; }

    public required string CorrelationId { get; set; }

    public int? UserId { get; set; }

    public required string IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public required string HttpMethod { get; set; }

    public required string Path { get; set; }

    public string? RedactedQueryString { get; set; }

    public int StatusCode { get; set; }

    public long DurationMs { get; set; }

    public DateTime TimestampUtc { get; set; }
}
