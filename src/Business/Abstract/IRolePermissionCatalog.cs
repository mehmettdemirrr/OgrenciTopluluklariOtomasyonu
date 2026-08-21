using Core.Aspects.Autofac;

namespace Business.Abstract;

/// <summary>
/// docs/MIMARI.md · A-17/Y-45: rol→izin haritası (izin kataloğu) — global ve durağan, bu yüzden
/// cache'lenir. Kullanıcı→rol ataması (IdentityGateway.GetPermissionsAsync içinde) BURADA değil,
/// her çağrıda taze okunur; yalnızca bu ayrım kullanıcı bazlı bir cache'in doğuracağı izin
/// sızıntısı riskini (aynı anahtarı paylaşan kullanıcılar) ortadan kaldırır.
/// </summary>
public interface IRolePermissionCatalog
{
    [CacheAspect(durationMinutes: 5)]
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(string roleName);
}
