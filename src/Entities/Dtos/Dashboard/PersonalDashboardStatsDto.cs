namespace Entities.Dtos.Dashboard;

/// <summary>docs/PLAN-V2.md · Faz 12 (K-25): çağıranın KENDİ öğrenci profiline bağlı sayılar.</summary>
public sealed class PersonalDashboardStatsDto
{
    public int MyClubCount { get; set; }

    public int MyPendingApplicationCount { get; set; }

    public int MyUpcomingEventCount { get; set; }
}
