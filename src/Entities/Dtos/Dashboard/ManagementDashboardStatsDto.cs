namespace Entities.Dtos.Dashboard;

/// <summary>
/// docs/PLAN-V2.md · Faz 12 (K-25): çağıranın rapor kapsamındaki (danışmanlık/officer-president/admin)
/// kulüpleri kapsayan sayılar. Kapsamı olmayan kullanıcılar (düz Member) için bu bölüm hiç üretilmez.
/// </summary>
public sealed class ManagementDashboardStatsDto
{
    public bool AllClubs { get; set; }

    public int ScopeClubCount { get; set; }

    public int ScopeMemberCount { get; set; }

    public int ScopePendingApplicationCount { get; set; }

    public int ScopeUpcomingEventCount { get; set; }
}
