using Entities;

namespace Business.Abstract;

/// <summary>
/// docs/PLAN-V2.md · Faz 11 (K-03/A-40): AccountManager'ın (kayıt/e-posta doğrulama/şifre sıfırlama/
/// parola değiştirme) ihtiyaç duyduğu asgari Identity yüzeyi — IIdentityGateway (login) ve
/// IIdentityAdminGateway'den (K-17 yönetim yazmaları) ayrı, üçüncü bir seam.
/// </summary>
public interface IAccountGateway
{
    Task<ApplicationUser?> FindByEmailAsync(string email);

    Task<ApplicationUser?> FindByIdAsync(int userId);

    /// <summary>Başarılıysa true. Y-25: Identity'nin İngilizce hata metinleri dışarı sızmaz.</summary>
    Task<bool> CreateUserAsync(ApplicationUser user, string password);

    Task AddToRoleAsync(ApplicationUser user, string roleName);

    /// <summary>docs/PLAN-V2.md · Y-54: token burada üretilir, çağıran (job) yalnızca userId taşır.</summary>
    Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);

    Task<bool> ConfirmEmailAsync(ApplicationUser user, string token);

    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

    Task<bool> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);

    Task<bool> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);

    Task<IReadOnlyCollection<string>> GetRoleNamesAsync(ApplicationUser user);
}
