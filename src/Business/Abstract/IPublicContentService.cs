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
        int pageIndex, int pageSize, string? search = null, int? categoryId = null, CancellationToken cancellationToken = default);

    Task<IDataResult<PublicClubDetailDto>> GetClubByIdAsync(int id, CancellationToken cancellationToken = default);

    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PagedResult<PublicEventListItemDto>>> GetEventsAsync(
        int? clubId, int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    [CacheAspect(durationMinutes: 10)]
    Task<IDataResult<PagedResult<PublicAnnouncementListItemDto>>> GetAnnouncementsAsync(
        int? clubId, int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);
}
