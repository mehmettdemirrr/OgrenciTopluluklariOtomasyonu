using Entities.Enums;

namespace Business.DTOs.Events;

public sealed class EventListItemDto
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? Capacity { get; set; }

    public EventStatus Status { get; set; }

    /// <summary>docs/MIMARI.md · A-49: yalnızca `Cancelled` durumunda dolu.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// docs/PLAN-V4.md §21.5 (Y-62): giriş yapmış öğrencinin bu etkinliğe kaydı var mı.
    /// <b>null</b> = bu listede hesaplanmadı — arayüz "kayıtlı değil" ile karıştırmasın diye
    /// bilinçli olarak `bool?`. Yalnızca `/api/events/upcoming` doldurur.
    /// </summary>
    public bool? IsRegistered { get; set; }
}
