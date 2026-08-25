using Core.DataAccess;
using Core.Utilities.Security;
using Entities.Dtos.Admin;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-08: sayfalama SQL'de, sonuç materyalize edilmiş liste olarak döner.</summary>
public sealed class EfIdentityAdminDal(AppDbContext context) : IIdentityAdminDal
{
    public async Task<PagedResult<RoleRowDto>> GetRolesPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Roles.AsNoTracking().OrderBy(r => r.Name);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(r => new RoleRowDto { Id = r.Id, Name = r.Name! })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<RoleRowDto>(items, totalCount, pageIndex, pageSize);
    }

    public Task<List<RolePermissionRowDto>> GetRolePermissionsAsync(IReadOnlyCollection<int> roleIds, CancellationToken cancellationToken = default) =>
        context.RoleClaims
            .AsNoTracking()
            .Where(c => roleIds.Contains(c.RoleId) && c.ClaimType == CurrentUserClaimTypes.Permission)
            .Select(c => new RolePermissionRowDto { RoleId = c.RoleId, Permission = c.ClaimValue! })
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<UserRowDto>> GetUsersPagedAsync(
        int pageIndex, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // A-56: arama artık ad ve soyadı da kapsıyor — yönetici kişiyi e-postasıyla değil
            // adıyla arayabilmeli. Filtreleme SQL'de (A-50/Y-62).
            query = query.Where(u =>
                u.Email!.Contains(search)
                || (u.FirstName != null && u.FirstName.Contains(search))
                || (u.LastName != null && u.LastName.Contains(search)));
        }

        query = query.OrderBy(u => u.Email);

        var now = DateTimeOffset.UtcNow;

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(u => new UserRowDto
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                // A-57: pasif = kilit geleceğe kurulmuş. Geçmişte kalan LockoutEnd "süresi dolmuş
                // kilit" demektir ve kullanıcı yeniden giriş yapabilir — pasif sayılmamalı.
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<UserRowDto>(items, totalCount, pageIndex, pageSize);
    }

    public Task<List<UserRoleRowDto>> GetUserRoleNamesAsync(IReadOnlyCollection<int> userIds, CancellationToken cancellationToken = default) =>
        context.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(context.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => new UserRoleRowDto { UserId = ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

    public Task<int> CountUsersInRoleAsync(int roleId, CancellationToken cancellationToken = default) =>
        context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == roleId, cancellationToken);
}
