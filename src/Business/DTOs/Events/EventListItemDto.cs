using Entities.Enums;

namespace Business.DTOs.Events;

public sealed class EventListItemDto
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    /// <summary>docs/MIMARI.md · A-71: null ise Description düz metin olarak gösterilir.</summary>
    public string? DescriptionJson { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? Capacity { get; set; }

    public EventStatus Status { get; set; }

    /// <summary>docs/MIMARI.md · K-38/A-65: arayüz "Üyelere Özel" rozetini bu alandan çizer.</summary>
    public EventAudience Audience { get; set; }

    /// <summary>docs/MIMARI.md · A-49: yalnızca `Cancelled` durumunda dolu.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-36: etkinlik afişi. Görünürlüğü `Public` olduğu için anonim
    /// `GET /api/files/{id}` ucundan servis edilir; DTO yalnızca kimliği taşır.
    /// </summary>
    public int? PosterFileId { get; set; }

    /// <summary>
    /// docs/PLAN-V4.md §21.5 (Y-62): giriş yapmış öğrencinin bu etkinliğe kaydı var mı.
    /// <b>null</b> = bu listede hesaplanmadı — arayüz "kayıtlı değil" ile karıştırmasın diye
    /// bilinçli olarak `bool?`. Yalnızca `/api/events/upcoming` doldurur.
    /// </summary>
    public bool? IsRegistered { get; set; }

    /// <summary>docs/MIMARI.md · K-46: düzenleyen topluluğun logosu (kart görseli). Null = logosuz kulüp.</summary>
    public int? ClubLogoFileId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-46: bu etkinliğe kayıtlı kişi sayısı — isim/öğrenci no taşımaz (Y-58).
    /// <b>null</b> = bu listede hesaplanmadı (IsRegistered ile aynı sözleşme): yalnızca detay ucu doldurur,
    /// liste uçları sayfa başına N sorgu açmasın diye boş bırakır.
    /// </summary>
    public int? ParticipantCount { get; set; }

    /// <summary>docs/MIMARI.md · A-77: yaklaşık görüntülenme sayısı; karar dayanağı değildir.</summary>
    public int ViewCount { get; set; }
}
