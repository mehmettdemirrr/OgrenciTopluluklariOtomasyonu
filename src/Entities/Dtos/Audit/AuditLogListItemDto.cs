using Entities.Enums;

namespace Entities.Dtos.Audit;

/// <summary>docs/PLAN-V2.md · Faz 13 (K-12): AuditSaveChangesInterceptor'ın yazdığı satırın okuma projeksiyonu.</summary>
public sealed class AuditLogListItemDto
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public required string EntityType { get; set; }

    public required string EntityId { get; set; }

    public AuditAction Action { get; set; }

    public DateTime TimestampUtc { get; set; }

    /// <summary>Y-26: hassas alan adları (parola/token/stamp) interceptor tarafından zaten hariç tutulmuştur.</summary>
    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    /// <summary>docs/PLAN-V3.md · K-28: aynı isteğin TrafficLog satırına bağlar.</summary>
    public string? CorrelationId { get; set; }
}
