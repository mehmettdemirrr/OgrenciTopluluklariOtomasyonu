using Core.Entities;

namespace Entities;

/// <summary>docs/MIMARI.md: topluluk duyurusu. Y-16: soft delete + query filter.</summary>
public sealed class Announcement : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public DateTime PublishedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
