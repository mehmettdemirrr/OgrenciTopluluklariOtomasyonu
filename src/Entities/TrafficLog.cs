namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-28/A-44: istek meta verisi — kullanıcı, IP, tarayıcı, method, URL, süre.
/// IEntity implemente etmez — AuditLog gibi generic repository üzerinden erişilmez, kendi kendini
/// loglamaz. Y-59: gövde/header/token asla yazılmaz; RedactedQueryString bilinen hassas anahtarları
/// redakte eder.
/// </summary>
public sealed class TrafficLog
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
