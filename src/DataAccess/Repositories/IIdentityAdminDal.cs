using Core.DataAccess;
using Entities.Dtos.Admin;

namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · K-17/Y-08: RoleManager/UserManager döngüsü sayfa başına N+1 üretir — okuma
/// tarafı bu yüzden generic repository'den değil, doğrudan SQL projeksiyonundan geçer. Yazma
/// tarafı (normalized name/ConcurrencyStamp tutarlılığı) burada değil, IIdentityAdminGateway'de
/// Identity API'siyle yapılır (Y-04: DAL iş kuralı taşımaz).
/// </summary>
public interface IIdentityAdminDal
{
    Task<PagedResult<RoleRowDto>> GetRolesPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<List<RolePermissionRowDto>> GetRolePermissionsAsync(IReadOnlyCollection<int> roleIds, CancellationToken cancellationToken = default);

    Task<PagedResult<UserRowDto>> GetUsersPagedAsync(int pageIndex, int pageSize, string? search, CancellationToken cancellationToken = default);

    Task<List<UserRoleRowDto>> GetUserRoleNamesAsync(IReadOnlyCollection<int> userIds, CancellationToken cancellationToken = default);

    Task<int> CountUsersInRoleAsync(int roleId, CancellationToken cancellationToken = default);
}
