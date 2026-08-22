namespace Business.Abstract;

/// <summary>
/// docs/MIMARI.md · K-17: RoleAdminManager'ın ihtiyaç duyduğu asgari Identity yazma yüzeyi
/// (IIdentityGateway'den ayrı, ikinci bir seam — o okuma/login içindir, bu yönetim yazmaları için).
/// Normalized name/ConcurrencyStamp tutarlılığı yalnızca Identity API'siyle korunur, DAL ile
/// rol/kullanıcı yazılmaz. Identity'nin İngilizce hata metinleri dışarı sızmaz (Y-25):
/// bool/int?/koleksiyon döner, Türkçe mesajı RoleAdminManager üretir.
/// </summary>
public interface IIdentityAdminGateway
{
    Task<bool> RoleExistsByNameAsync(string name);

    Task<bool> RoleExistsByIdAsync(int roleId);

    /// <summary>Başarılıysa yeni rolün Id'sini, aksi hâlde null döner.</summary>
    Task<int?> CreateRoleAsync(string name);

    /// <summary>Rol bulunamazsa false döner.</summary>
    Task<bool> DeleteRoleAsync(int roleId);

    /// <summary>Cache'siz/taze — K-17 düzenleme ekranının "mevcut durum" kaynağı.</summary>
    Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(int roleId);

    /// <summary>Diff uygular: eksik izinler eklenir, fazlalar kaldırılır (Add/RemoveClaim).</summary>
    Task SetRolePermissionsAsync(int roleId, IReadOnlyCollection<string> permissions);

    Task<IReadOnlyCollection<string>> GetUserRoleNamesAsync(int userId);

    /// <summary>Kullanıcı bulunamazsa false döner.</summary>
    Task<bool> SetUserRolesAsync(int userId, IReadOnlyCollection<string> roleNames);

    /// <summary>docs/PLAN-V2.md · Faz 11: yönetici kullanıcıyı doğrudan oluşturur — EmailConfirmed=true (K-03 akışını atlar).</summary>
    Task<int?> CreateUserAsync(string email, string password, IReadOnlyCollection<string> roleNames);

    /// <summary>Kullanıcı bulunamazsa false döner.</summary>
    Task<bool> SetLockoutAsync(int userId, bool locked);
}
