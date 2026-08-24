namespace Core.Entities;

/// <summary>
/// Generic repository ve entity konfigürasyonlarının çalışabilmesi için
/// Entities katmanındaki tüm sınıfların uyması gereken taban sözleşme.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// docs/MIMARI.md · Y-64: sayfalamanın son kırıcısı (tie-breaker). Sırasız <c>Skip</c>/<c>Take</c>
    /// SQL Server'da <c>ORDER BY (SELECT 1)</c>'e düşer ve sayfalar arasında satır tekrarına yol açar.
    /// </summary>
    int Id { get; }
}
