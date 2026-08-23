namespace Business.DTOs.Traffic;

/// <summary>docs/PLAN-V3.md · K-28: RequestLoggingMiddleware'in HttpContext'ten topladığı ham alanlar.</summary>
public sealed class RecordTrafficLogRequestDto
{
    public required string CorrelationId { get; set; }

    public int? UserId { get; set; }

    public required string IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public required string HttpMethod { get; set; }

    public required string Path { get; set; }

    public string? RedactedQueryString { get; set; }

    public int StatusCode { get; set; }

    public long DurationMs { get; set; }
}
