using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · A-15: (EventId, StudentId) unique index.
/// Y-16: soft delete + query filter.
/// </summary>
public sealed class EventParticipation : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int StudentId { get; set; }

    public DateTime RegisteredAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
