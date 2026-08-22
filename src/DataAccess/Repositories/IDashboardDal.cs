using Entities.Dtos.Dashboard;

namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · Y-42/Y-08: dashboard sayıları generic repository ile bellekte toplanmaz —
/// COUNT SQL'de yapılır, sonuç doğrudan dashboard DTO'suna projekte edilir (IReportDal precedent'i).
/// </summary>
public interface IDashboardDal
{
    Task<PersonalDashboardStatsDto> GetPersonalStatsAsync(int studentId, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>allClubs true ise clubIds yok sayılır (tüm kulüpler taranır).</summary>
    Task<ManagementDashboardStatsDto> GetManagementStatsAsync(
        bool allClubs, IReadOnlyCollection<int> clubIds, DateTime nowUtc, CancellationToken cancellationToken = default);
}
