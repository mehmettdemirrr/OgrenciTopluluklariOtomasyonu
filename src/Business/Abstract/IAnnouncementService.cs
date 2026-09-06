using Business.DTOs.Announcements;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>docs/PLAN-V2.md §10.4: kulüp duyurusu + sistem duyurusu (A-43, Y-57).</summary>
public interface IAnnouncementService
{
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    Task<IDataResult<PagedResult<AnnouncementListItemDto>>> GetForClubAsync(int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Sistem + kulüp duyurularının birleşik akışı, tarihe göre azalan.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    Task<IDataResult<PagedResult<AnnouncementListItemDto>>> GetFeedAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>Y-23: yalnızca kulübün danışmanı veya güncel dönemde Officer/President üyesi.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.AnnouncementsWrite)]
    [ValidationAspect(typeof(CreateAnnouncementRequestValidator))]
    [CacheRemoveAspect("PublicContentManager.")]
    [TransactionAspect]
    Task<IDataResult<int>> CreateAsync(int clubId, CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.AnnouncementsWrite)]
    [ValidationAspect(typeof(UpdateAnnouncementRequestValidator))]
    [CacheRemoveAspect("PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> UpdateAsync(int announcementId, UpdateAnnouncementRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Y-16: soft delete.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.AnnouncementsWrite)]
    [CacheRemoveAspect("PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> DeleteAsync(int announcementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-47: düzenleme SAYFASININ veri kaynağı — adres doğrudan açılabildiği için
    /// tekil okuma gerekir. Kapı yazma kapısının aynısıdır (EnsureCanManageAsync): düzenleyebilen okur.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.AnnouncementsWrite)]
    Task<IDataResult<AnnouncementListItemDto>> GetByIdAsync(int announcementId, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md §10.1: kulübe bağlı olmayan sistem duyurusu — yalnızca Admin.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.AnnouncementsGlobal)]
    [ValidationAspect(typeof(CreateAnnouncementRequestValidator))]
    [CacheRemoveAspect("PublicContentManager.")]
    [TransactionAspect]
    Task<IDataResult<int>> CreateGlobalAsync(CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-42: `FileManager.UploadAnnouncementImageAsync`'in kapak görseli yetki kapısı.
    /// `CreateAsync`/`UpdateAsync`/`DeleteAsync`'in kullandığı aynı `EnsureAnnouncementWriteAccessAsync`
    /// zincirini çağırır — ikinci bir kopya yazmak, iki yerin er ya da geç ayrışması demektir.
    /// Controller ucu YOKTUR; yalnızca FileManager çağırır, bu yüzden [SecuredOperation] taşımaz.
    /// </summary>
    Task<IResult> EnsureCanManageAsync(int announcementId, CancellationToken cancellationToken = default);
}
