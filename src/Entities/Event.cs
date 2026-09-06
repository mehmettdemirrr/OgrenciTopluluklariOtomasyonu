using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · A-25/A-15/K-05: etkinlik, durum makinesi, kontenjan rowversion.
/// Y-16: soft delete + query filter.
/// </summary>
public sealed class Event : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-43/A-71: biçimlendirilmiş açıklamanın düğüm ağacı (JSON).
    /// Null = düz metin açıklama. Description bu alanın düz metin aynasıdır.
    /// </summary>
    public string? DescriptionJson { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    /// <summary>Null ise kontenjan sınırsız.</summary>
    public int? Capacity { get; set; }

    public EventStatus Status { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-38/A-65/Y-72: katılım kitlesi. Nullable değil — yazma anında zorunlu.
    /// Varsayılan Public: migration mevcut satırlara bu değeri yazar, bugünkü davranış korunur.
    /// </summary>
    public EventAudience Audience { get; set; }

    /// <summary>docs/MIMARI.md · A-49: iptal gerekçesi — katılımcılara gönderilen e-postada ve etkinlik sayfasında görünür.</summary>
    public string? CancellationReason { get; set; }

    public int? PosterFileId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-46/A-77: yaklaşık görüntülenme sayısı. Yalnızca
    /// `IEventViewDal.IncrementAsync` (tek SQL UPDATE) artırır; okuma yolu dokunmaz.
    /// </summary>
    public int ViewCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>docs/MIMARI.md · A-15: kontenjan aşımı için eşzamanlılık kontrolü.</summary>
    public byte[] RowVersion { get; set; } = null!;
}
