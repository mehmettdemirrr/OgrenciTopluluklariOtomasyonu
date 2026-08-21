using Business.DTOs.Admin;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>docs/MIMARI.md · K-17: yetki matrisi (rol + izin + kullanıcı-rol ataması).</summary>
public interface IRoleAdminService
{
    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    Task<IDataResult<IReadOnlyCollection<PermissionCatalogItemDto>>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    Task<IDataResult<PagedResult<RoleListItemDto>>> GetRolesPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    [ValidationAspect(typeof(CreateRoleRequestValidator))]
    [CacheRemoveAspect("RolePermissionCatalog.")]
    [TransactionAspect]
    Task<IDataResult<int>> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    [CacheRemoveAspect("RolePermissionCatalog.")]
    [TransactionAspect]
    Task<IResult> DeleteRoleAsync(int roleId, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    [ValidationAspect(typeof(SetRolePermissionsRequestValidator))]
    [CacheRemoveAspect("RolePermissionCatalog.")]
    [TransactionAspect]
    Task<IResult> SetRolePermissionsAsync(int roleId, SetRolePermissionsRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    Task<IDataResult<PagedResult<UserListItemDto>>> GetUsersPagedAsync(
        int pageIndex, int pageSize, string? search, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · Y-45 "her uç" lafzı: kullanıcı→rol ataması kendisi cache'lenmiyor,
    /// bu yüzden cache düşürme burada savunma amaçlıdır (rol→izin haritası hiç değişmedi).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.RolesManage)]
    [ValidationAspect(typeof(SetUserRolesRequestValidator))]
    [CacheRemoveAspect("RolePermissionCatalog.")]
    [TransactionAspect]
    Task<IResult> SetUserRolesAsync(int userId, SetUserRolesRequestDto request, CancellationToken cancellationToken = default);
}
