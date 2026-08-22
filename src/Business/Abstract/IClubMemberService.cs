using Business.DTOs.Clubs;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>
/// docs/PLAN-V2.md §9: kulüp üye rollerini yönetir — `ClubRole.Officer/President`in ilk yazarı.
/// Y-23: `memberships.read`/`memberships.write` izinleri Admin/ClubOfficer/Advisor arasında paylaşılır;
/// kapsam kararı (kimin HANGİ kulüpte işlem yapabileceği) burada, EventManager'daki aynı desenle verilir.
/// </summary>
public interface IClubMemberService
{
    /// <summary>docs/PLAN-V2.md · Faz 12: çağıranın kendi üyelikleri — EventParticipationService.GetMineAsync precedent'i.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    Task<IDataResult<IReadOnlyCollection<MyClubMembershipDto>>> GetMineAsync(CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.MembershipsRead)]
    Task<IDataResult<PagedResult<ClubMemberListItemDto>>> GetMembersPagedAsync(
        int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.MembershipsWrite)]
    [TransactionAspect]
    Task<IResult> SetRoleAsync(int clubId, int membershipId, SetClubRoleRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.MembershipsWrite)]
    [TransactionAspect]
    Task<IResult> RemoveMemberAsync(int clubId, int membershipId, CancellationToken cancellationToken = default);
}
