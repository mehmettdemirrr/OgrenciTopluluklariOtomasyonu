using System.Globalization;
using Business.Abstract;
using Entities;
using Microsoft.AspNetCore.Identity;

namespace Business.Concrete;

public sealed class AccountGateway(UserManager<ApplicationUser> userManager) : IAccountGateway
{
    public Task<ApplicationUser?> FindByEmailAsync(string email) => userManager.FindByEmailAsync(email);

    public Task<ApplicationUser?> FindByIdAsync(int userId) =>
        userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public async Task<bool> CreateUserAsync(ApplicationUser user, string password) =>
        (await userManager.CreateAsync(user, password).ConfigureAwait(false)).Succeeded;

    public Task AddToRoleAsync(ApplicationUser user, string roleName) => userManager.AddToRoleAsync(user, roleName);

    public Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user) => userManager.GenerateEmailConfirmationTokenAsync(user);

    public async Task<bool> ConfirmEmailAsync(ApplicationUser user, string token) =>
        (await userManager.ConfirmEmailAsync(user, token).ConfigureAwait(false)).Succeeded;

    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user) => userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<bool> ResetPasswordAsync(ApplicationUser user, string token, string newPassword) =>
        (await userManager.ResetPasswordAsync(user, token, newPassword).ConfigureAwait(false)).Succeeded;

    public async Task<bool> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword) =>
        (await userManager.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false)).Succeeded;

    public async Task<IReadOnlyCollection<string>> GetRoleNamesAsync(ApplicationUser user) =>
        (await userManager.GetRolesAsync(user).ConfigureAwait(false)).ToArray();

    public async Task<bool> SetNameAsync(int userId, string? firstName, string? lastName)
    {
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        user.FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();

        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }
}
