using Business.Abstract;
using Business.Constants;
using Business.DTOs.Admin;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using DataAccess.Repositories;
using DataAccess.Seed;

namespace Business.Concrete;

public sealed class RoleAdminManager(
    IIdentityAdminDal identityAdminDal,
    IIdentityAdminGateway identityAdminGateway,
    ICurrentUser currentUser) : IRoleAdminService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public Task<IDataResult<IReadOnlyCollection<PermissionCatalogItemDto>>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PermissionCatalogItemDto> items = IdentitySeedData.AllPermissionCodes()
            .Select(code =>
            {
                var (displayName, description) = PermissionCatalog.Descriptions.TryGetValue(code, out var entry)
                    ? entry
                    : (code, string.Empty);

                return new PermissionCatalogItemDto { Code = code, DisplayName = displayName, Description = description };
            })
            .ToList();

        return Task.FromResult<IDataResult<IReadOnlyCollection<PermissionCatalogItemDto>>>(DataResult<IReadOnlyCollection<PermissionCatalogItemDto>>.Success(items));
    }

    public async Task<IDataResult<PagedResult<RoleListItemDto>>> GetRolesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await identityAdminDal.GetRolesPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken).ConfigureAwait(false);

        var roleIds = paged.Items.Select(r => r.Id).ToList();
        var permissionRows = await identityAdminDal.GetRolePermissionsAsync(roleIds, cancellationToken).ConfigureAwait(false);
        var permissionsByRole = permissionRows
            .GroupBy(p => p.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(p => p.Permission).ToArray());

        var items = paged.Items
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                IsSystemRole = IdentitySeedData.IsSystemRole(r.Id),
                Permissions = permissionsByRole.GetValueOrDefault(r.Id, []),
            })
            .ToList();

        var result = new PagedResult<RoleListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<RoleListItemDto>>.Success(result);
    }

    public async Task<IDataResult<int>> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await identityAdminGateway.RoleExistsByNameAsync(name).ConfigureAwait(false))
        {
            return DataResult<int>.Conflict(Messages.RoleNameTaken);
        }

        var roleId = await identityAdminGateway.CreateRoleAsync(name).ConfigureAwait(false);
        if (roleId is not { } createdRoleId)
        {
            return DataResult<int>.Conflict(Messages.RoleNameTaken);
        }

        return DataResult<int>.Success(createdRoleId, Messages.RoleCreated);
    }

    public async Task<IResult> DeleteRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        if (IdentitySeedData.IsSystemRole(roleId))
        {
            return Result.Conflict(Messages.SystemRoleCannotBeDeleted);
        }

        var userCount = await identityAdminDal.CountUsersInRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (userCount > 0)
        {
            return Result.Conflict(Messages.RoleHasUsers);
        }

        var deleted = await identityAdminGateway.DeleteRoleAsync(roleId).ConfigureAwait(false);
        if (!deleted)
        {
            return Result.NotFound(Messages.RoleNotFound);
        }

        return Result.Success(Messages.RoleDeleted);
    }

    public async Task<IResult> SetRolePermissionsAsync(
        int roleId, SetRolePermissionsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!await identityAdminGateway.RoleExistsByIdAsync(roleId).ConfigureAwait(false))
        {
            return Result.NotFound(Messages.RoleNotFound);
        }

        var unknown = request.Permissions.Except(IdentitySeedData.AllPermissionCodes()).Any();
        if (unknown)
        {
            return Result.ValidationError(Messages.UnknownPermissionCode);
        }

        // Y-03: Admin, K-17 ekranına erişimini kaybedecek şekilde kendi kendini kilitleyemez.
        if (roleId == IdentitySeedData.AdminRoleId && !request.Permissions.Contains(IdentitySeedData.Permissions.RolesManage))
        {
            return Result.Conflict(Messages.AdminRoleMustKeepRolesManage);
        }

        await identityAdminGateway.SetRolePermissionsAsync(roleId, request.Permissions).ConfigureAwait(false);

        return Result.Success(Messages.PermissionsUpdated);
    }

    public async Task<IDataResult<PagedResult<UserListItemDto>>> GetUsersPagedAsync(
        int pageIndex, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var paged = await identityAdminDal
            .GetUsersPagedAsync(pageIndex, ClampPageSize(pageSize), search, cancellationToken)
            .ConfigureAwait(false);

        var userIds = paged.Items.Select(u => u.Id).ToList();
        var roleRows = await identityAdminDal.GetUserRoleNamesAsync(userIds, cancellationToken).ConfigureAwait(false);
        var rolesByUser = roleRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(r => r.RoleName).ToArray());

        var items = paged.Items
            .Select(u => new UserListItemDto { Id = u.Id, Email = u.Email, Roles = rolesByUser.GetValueOrDefault(u.Id, []) })
            .ToList();

        var result = new PagedResult<UserListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<UserListItemDto>>.Success(result);
    }

    public async Task<IResult> SetUserRolesAsync(int userId, SetUserRolesRequestDto request, CancellationToken cancellationToken = default)
    {
        // Y-03: kendi Admin rolünü kaldırarak K-17 ekranına erişimini kaybetmesin.
        if (currentUser.UserId == userId)
        {
            var ownRoles = await identityAdminGateway.GetUserRoleNamesAsync(userId).ConfigureAwait(false);
            if (ownRoles.Contains(IdentitySeedData.AdminRoleName) && !request.RoleNames.Contains(IdentitySeedData.AdminRoleName))
            {
                return Result.Conflict(Messages.CannotRemoveOwnAdminRole);
            }
        }

        foreach (var roleName in request.RoleNames)
        {
            if (!await identityAdminGateway.RoleExistsByNameAsync(roleName).ConfigureAwait(false))
            {
                return Result.NotFound(Messages.RoleNotFound);
            }
        }

        var updated = await identityAdminGateway.SetUserRolesAsync(userId, request.RoleNames).ConfigureAwait(false);
        if (!updated)
        {
            return Result.NotFound(Messages.UserNotFound);
        }

        return Result.Success(Messages.RolesUpdated);
    }

    public async Task<IDataResult<int>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        foreach (var roleName in request.RoleNames)
        {
            if (!await identityAdminGateway.RoleExistsByNameAsync(roleName).ConfigureAwait(false))
            {
                return DataResult<int>.NotFound(Messages.RoleNotFound);
            }
        }

        var userId = await identityAdminGateway.CreateUserAsync(request.Email.Trim(), request.Password, request.RoleNames).ConfigureAwait(false);
        return userId is { } id ? DataResult<int>.Success(id, Messages.UserCreated) : DataResult<int>.Conflict(Messages.UserCreationFailed);
    }

    public async Task<IResult> SetLockoutAsync(int userId, SetLockoutRequestDto request, CancellationToken cancellationToken = default)
    {
        // Y-03: Admin kendi hesabını kilitleyerek K-17 ekranına erişimini kaybedecek şekilde
        // kendi kendini kilitleyemez — CannotRemoveOwnAdminRole ile aynı öz-kısıtlama sınıfı.
        if (request.Locked && currentUser.UserId == userId)
        {
            return Result.Conflict(Messages.CannotLockOwnAccount);
        }

        var updated = await identityAdminGateway.SetLockoutAsync(userId, request.Locked).ConfigureAwait(false);
        return updated ? Result.Success(Messages.LockoutUpdated) : Result.NotFound(Messages.UserNotFound);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
