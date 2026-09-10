using Business.DTOs.Public;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// docs/PLAN-V2.md · Faz 14 (A-42): anonim vitrin yüzeyinin TEK servisi. [SecuredOperation] kasıtlı
/// olarak yok; filtreler (IsActive, Status==Published, Visibility==Public) kodda sabit (Y-58).
/// Kimlik doğrulamalı servislerin (IClubService/IEventService/IAnnouncementService) sorguları veya
/// DTO'ları buradan asla çağrılmaz/yeniden kullanılmaz — ayrı ve denetlenebilir kalması bilinçli.
/// </summary>
public interface IPublicContentService
{
    // A-50: `search` cache anahtarının parçası olur (CacheAspectHandler tüm argümanları yazar);
    // anahtar sayısını A-54'ün SizeLimit'i, metin uzunluğunu SearchTerm.MaxLength sınırlar.
    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PagedResult<PublicClubListItemDto>>> GetClubsAsync(
        int pageIndex, int pageSize, string? search = null, int? categoryId = null, string? letter = null, CancellationToken cancellationToken = default);

    Task<IDataResult<PublicClubDetailDto>> GetClubByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · A-82: sayacı tek SQL cümlesiyle artırır; okuma yolu bunu çağırmaz.</summary>
    Task<IResult> RegisterClubViewAsync(int id, CancellationToken cancellationToken = default);

    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PagedResult<PublicEventListItemDto>>> GetEventsAsync(
        int? clubId, int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anasayfa takvimi: yayındaki herkese açık etkinlikler açık, ClubMembers kitleli olanlar
    /// kilitli tarih dilimi olarak döner (başlık/id yok — Y-72). Kimlik yok; önbelleklenebilir.
    /// </summary>
    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<IReadOnlyList<PublicCalendarEventDto>>> GetCalendarEventsAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · K-46/Y-82: vitrin listesiyle aynı filtre; eşleşmezse NotFound. Sayaç okumadan artmaz (A-77), bu yüzden [CacheAspect] YOK.</summary>
    Task<IDataResult<PublicEventDetailDto>> GetEventByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · A-77: sayacı tek SQL cümlesiyle artırır; okuma yolu bunu çağırmaz.</summary>
    Task<IResult> RegisterEventViewAsync(int id, CancellationToken cancellationToken = default);

    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PagedResult<PublicAnnouncementListItemDto>>> GetAnnouncementsAsync(
        int? clubId, int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>Vitrin listesiyle aynı Public filtresi; eşleşmezse varlığı sızdırmadan NotFound.</summary>
    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PublicAnnouncementListItemDto>> GetAnnouncementByIdAsync(int id, CancellationToken cancellationToken = default);

    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PublicStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default);

    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<IReadOnlyList<PublicClubCategoryDto>>> GetClubCategoriesAsync(CancellationToken cancellationToken = default);
}
