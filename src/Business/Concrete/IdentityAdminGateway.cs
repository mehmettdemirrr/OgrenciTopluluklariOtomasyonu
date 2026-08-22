using System.Globalization;
using System.Security.Claims;
using Business.Abstract;
using Core.Utilities.Security;
using Entities;
using Microsoft.AspNetCore.Identity;

namespace Business.Concrete;

public sealed class IdentityAdminGateway(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    : IIdentityAdminGateway
{
    public Task<bool> RoleExistsByNameAsync(string name) => roleManager.RoleExistsAsync(name);

    public async Task<bool> RoleExistsByIdAsync(int roleId) =>
        await FindRoleAsync(roleId).ConfigureAwait(false) is not null;

    public async Task<int?> CreateRoleAsync(string name)
    {
        var role = new ApplicationRole { Name = name };
        var result = await roleManager.CreateAsync(role).ConfigureAwait(false);
        return result.Succeeded ? role.Id : null;
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        var role = await FindRoleAsync(roleId).ConfigureAwait(false);
        if (role is null)
        {
            return false;
        }

        var result = await roleManager.DeleteAsync(role).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(int roleId)
    {
        var role = await FindRoleAsync(roleId).ConfigureAwait(false);
        if (role is null)
        {
            return [];
        }

        return await PermissionValuesAsync(role).ConfigureAwait(false);
    }

    public async Task SetRolePermissionsAsync(int roleId, IReadOnlyCollection<string> permissions)
    {
        var role = await FindRoleAsync(roleId).ConfigureAwait(false);
        if (role is null)
        {
            return;
        }

        var existingClaims = (await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
            .Where(c => c.Type == CurrentUserClaimTypes.Permission)
            .ToList();

        foreach (var claim in existingClaims.Where(c => !permissions.Contains(c.Value)))
        {
            await roleManager.RemoveClaimAsync(role, claim).ConfigureAwait(false);
        }

        var existingValues = existingClaims.Select(c => c.Value).ToHashSet();
        foreach (var permission in permissions.Where(p => !existingValues.Contains(p)))
        {
            await roleManager.AddClaimAsync(role, new Claim(CurrentUserClaimTypes.Permission, permission)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyCollection<string>> GetUserRoleNamesAsync(int userId)
    {
        var user = await FindUserAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return [];
        }

        return (await userManager.GetRolesAsync(user).ConfigureAwait(false)).ToArray();
    }

    public async Task<bool> SetUserRolesAsync(int userId, IReadOnlyCollection<string> roleNames)
    {
        var user = await FindUserAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        var currentRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var toRemove = currentRoles.Except(roleNames).ToArray();
        var toAdd = roleNames.Except(currentRoles).ToArray();

        if (toRemove.Length > 0)
        {
            await userManager.RemoveFromRolesAsync(user, toRemove).ConfigureAwait(false);
        }

        if (toAdd.Length > 0)
        {
            await userManager.AddToRolesAsync(user, toAdd).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<int?> CreateUserAsync(string email, string password, IReadOnlyCollection<string> roleNames)
    {
        // K-17 admin akışı Faz 11'in e-posta doğrulama zincirini atlar — yönetici zaten kimliği doğrulanmış
        // bir aktördür, hesabı kendisi oluşturuyor.
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return null;
        }

        if (roleNames.Count > 0)
        {
            await userManager.AddToRolesAsync(user, roleNames).ConfigureAwait(false);
        }

        return user.Id;
    }

    public async Task<bool> SetLockoutAsync(int userId, bool locked)
    {
        var user = await FindUserAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        await userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null).ConfigureAwait(false);
        return true;
    }

    private Task<ApplicationRole?> FindRoleAsync(int roleId) =>
        roleManager.FindByIdAsync(roleId.ToString(CultureInfo.InvariantCulture));

    private Task<ApplicationUser?> FindUserAsync(int userId) =>
        userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));

    private async Task<IReadOnlyCollection<string>> PermissionValuesAsync(ApplicationRole role)
    {
        var claims = await roleManager.GetClaimsAsync(role).ConfigureAwait(false);
        return claims.Where(c => c.Type == CurrentUserClaimTypes.Permission).Select(c => c.Value).ToArray();
    }
}
