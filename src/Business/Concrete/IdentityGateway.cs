using Business.Abstract;
using Core.Utilities.Security;
using Entities;
using Microsoft.AspNetCore.Identity;

namespace Business.Concrete;

public sealed class IdentityGateway(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    : IIdentityGateway
{
    public Task<ApplicationUser?> FindByEmailAsync(string email) => userManager.FindByEmailAsync(email);

    public Task<ApplicationUser?> FindByIdAsync(int userId) =>
        userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) => userManager.CheckPasswordAsync(user, password);

    public Task<bool> IsLockedOutAsync(ApplicationUser user) => userManager.IsLockedOutAsync(user);

    public Task AccessFailedAsync(ApplicationUser user) => userManager.AccessFailedAsync(user);

    public Task ResetAccessFailedCountAsync(ApplicationUser user) => userManager.ResetAccessFailedCountAsync(user);

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(ApplicationUser user)
    {
        var roleNames = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var permissions = new HashSet<string>();

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (role is null)
            {
                continue;
            }

            var claims = await roleManager.GetClaimsAsync(role).ConfigureAwait(false);
            foreach (var claim in claims.Where(c => c.Type == CurrentUserClaimTypes.Permission))
            {
                permissions.Add(claim.Value);
            }
        }

        return permissions;
    }
}
