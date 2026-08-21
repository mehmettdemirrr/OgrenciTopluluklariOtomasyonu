using Business.Abstract;
using Core.Utilities.Security;
using Entities;
using Microsoft.AspNetCore.Identity;

namespace Business.Concrete;

public sealed class RolePermissionCatalog(RoleManager<ApplicationRole> roleManager) : IRolePermissionCatalog
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
        if (role is null)
        {
            return [];
        }

        var claims = await roleManager.GetClaimsAsync(role).ConfigureAwait(false);
        return claims
            .Where(c => c.Type == CurrentUserClaimTypes.Permission)
            .Select(c => c.Value)
            .ToArray();
    }
}
