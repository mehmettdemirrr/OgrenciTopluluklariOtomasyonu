using Entities.Enums;

namespace Business.DTOs.Announcements;

public sealed class UpdateAnnouncementRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public AnnouncementVisibility Visibility { get; set; }

    /// <summary>docs/MIMARI.md · K-42/A-71: biçimlendirilmiş içerik ağacı. Null = düz metin duyuru.</summary>
    public string? ContentJson { get; set; }
}
