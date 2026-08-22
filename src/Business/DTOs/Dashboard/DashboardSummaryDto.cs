using Entities.Dtos.Dashboard;
using Entities.Dtos.Reports;

namespace Business.DTOs.Dashboard;

/// <summary>docs/PLAN-V2.md · Faz 12 (K-25): rol farkında özet — GET /api/dashboard'un tek dönüş tipi.</summary>
public sealed class DashboardSummaryDto
{
    public required PersonalDashboardStatsDto Personal { get; init; }

    /// <summary>Danışmanlık/officer-president/admin kapsamı olan kullanıcılarda dolu, düz üyede null.</summary>
    public ManagementDashboardStatsDto? Management { get; init; }

    /// <summary>Yalnızca <see cref="Management"/> dolu olduğunda anlamlıdır; aksi hâlde boş liste.</summary>
    public required IReadOnlyCollection<TermSummaryRowDto> TermTrend { get; init; }
}
