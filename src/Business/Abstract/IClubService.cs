using Business.DTOs.Clubs;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

public interface IClubService
{
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    [CacheAspect(durationMinutes: 5)]
    /// <summary>A-50: `search` ad üzerinde SQL tarafında; `isActive` null ise aktif/pasif ayrımı yapılmaz; `categoryId` null ise kategori filtresi yok (K-35).</summary>
    Task<IDataResult<PagedResult<ClubListItemDto>>> GetListPagedAsync(
        int pageIndex, int pageSize, string? search = null, bool? isActive = null, int? categoryId = null,
        CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    Task<IDataResult<ClubDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md §9: clubs.write ilk kez kullanılır — ölü kodu (Officer/President) canlandıran fazın girişi.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsWrite)]
    [ValidationAspect(typeof(CreateClubRequestValidator))]
    [CacheRemoveAspect("ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IDataResult<int>> CreateAsync(CreateClubRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ClubsWrite)]
    [ValidationAspect(typeof(UpdateClubRequestValidator))]
    [CacheRemoveAspect("ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> UpdateAsync(int clubId, UpdateClubRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ClubsWrite)]
    [CacheRemoveAspect("ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> SetStatusAsync(int clubId, SetClubStatusRequestDto request, CancellationToken cancellationToken = default);
}
