using Entities.Enums;

namespace Business.DTOs.Announcements;

public sealed class CreateAnnouncementRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>docs/PLAN-V2.md · Y-57: varsayılana güvenilmez, arayüz açıkça seçtirir.</summary>
    public AnnouncementVisibility Visibility { get; set; }
}
