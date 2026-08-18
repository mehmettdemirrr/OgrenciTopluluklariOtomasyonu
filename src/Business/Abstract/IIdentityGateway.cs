using Entities;

namespace Business.Abstract;

/// <summary>
/// UserManager/RoleManager'ın 8 parametreli constructor'ını doğrudan mock'lamak yerine
/// AuthManager'ın ihtiyaç duyduğu asgari yüzeyi açan seam (A-20 testlerinin Moq ile mümkün olması için).
/// </summary>
public interface IIdentityGateway
{
    Task<ApplicationUser?> FindByEmailAsync(string email);

    Task<ApplicationUser?> FindByIdAsync(int userId);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    Task<bool> IsLockedOutAsync(ApplicationUser user);

    Task AccessFailedAsync(ApplicationUser user);

    Task ResetAccessFailedCountAsync(ApplicationUser user);

    /// <summary>docs/MIMARI.md · K-01: izinler her refresh'te yeniden çözülür — rol→rol claim'i→düz liste.</summary>
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(ApplicationUser user);
}
