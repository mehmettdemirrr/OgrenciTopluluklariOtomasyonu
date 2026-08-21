using Entities.Enums;

namespace Business.DTOs.Announcements;

public sealed class UpdateAnnouncementRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public AnnouncementVisibility Visibility { get; set; }
}
