using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md: topluluk duyurusu. Y-16: soft delete + query filter.
/// docs/PLAN-V2.md · A-43: null ClubId sistem duyurusudur (announcements.global gerektirir).
/// </summary>
public sealed class Announcement : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>Null ise sistem duyurusu (kulübe bağlı değil).</summary>
    public int? ClubId { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    /// <summary>docs/PLAN-V2.md · Y-57: anonim vitrin ucu yalnızca Public görünürlüğü döndürür.</summary>
    public AnnouncementVisibility Visibility { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-42/A-71: biçimlendirilmiş içeriğin düğüm ağacı (JSON).
    /// Null = eski/düz metin duyuru; o durumda Content olduğu gibi gösterilir.
    /// Content bu alanın düz metin aynasıdır — arama ve e-posta oradan okur.
    /// </summary>
    public string? ContentJson { get; set; }

    /// <summary>docs/MIMARI.md · K-42: kapak görseli. Null = görselsiz duyuru.</summary>
    public int? ImageFileId { get; set; }

    public DateTime PublishedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
