using Entities.Enums;

namespace Business.DTOs.Announcements;

public sealed class AnnouncementListItemDto
{
    public int Id { get; set; }

    /// <summary>Null ise sistem duyurusu.</summary>
    public int? ClubId { get; set; }

    public string? ClubName { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public AnnouncementVisibility Visibility { get; set; }

    public DateTime PublishedAtUtc { get; set; }
}
