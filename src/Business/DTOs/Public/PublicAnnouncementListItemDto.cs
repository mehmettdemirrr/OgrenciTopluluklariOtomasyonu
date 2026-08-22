namespace Business.DTOs.Public;

public sealed class PublicAnnouncementListItemDto
{
    public int Id { get; set; }

    public int? ClubId { get; set; }

    public string? ClubName { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public DateTime PublishedAtUtc { get; set; }
}
