using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-12/A-33/Y-44: denetim izi.
/// IEntity implemente etmez — audit kendisi audit edilmez, generic repository üzerinden erişilmez.
/// </summary>
public sealed class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public required string EntityType { get; set; }

    public required string EntityId { get; set; }

    public AuditAction Action { get; set; }

    public DateTime TimestampUtc { get; set; }

    /// <summary>Değişen alanların eski değerlerinin JSON serileştirmesi. Insert'te null.</summary>
    public string? OldValues { get; set; }

    /// <summary>Değişen alanların yeni değerlerinin JSON serileştirmesi. Delete'te null.</summary>
    public string? NewValues { get; set; }
}
