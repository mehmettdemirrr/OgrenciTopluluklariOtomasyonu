using Entities.Enums;

namespace Business.DTOs.Announcements;

public sealed class CreateAnnouncementRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>docs/PLAN-V2.md · Y-57: varsayılana güvenilmez, arayüz açıkça seçtirir.</summary>
    public AnnouncementVisibility Visibility { get; set; }

    /// <summary>docs/MIMARI.md · K-42/A-71: biçimlendirilmiş içerik ağacı. Null = düz metin duyuru.</summary>
    public string? ContentJson { get; set; }
}
