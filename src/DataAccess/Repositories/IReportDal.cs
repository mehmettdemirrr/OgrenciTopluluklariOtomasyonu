using Entities.Dtos.Reports;

namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · Y-42/Y-08: aggregate rapor sorguları generic repository'den GEÇMEZ — toplama
/// SQL'de yapılır, sonuç doğrudan rapor DTO'suna projekte edilir, materyalize liste döner.
/// </summary>
public interface IReportDal
{
    /// <summary>clubIdScope null ise tüm kulüpler (reports.read.all), aksi hâlde yalnızca listelenen kulüpler.</summary>
    Task<List<TermSummaryRowDto>> GetTermSummaryAsync(IReadOnlyCollection<int>? clubIdScope, CancellationToken cancellationToken = default);

    Task<List<ClubMemberExportRowDto>> GetClubMemberRowsAsync(int clubId, int academicTermId, CancellationToken cancellationToken = default);

    Task<List<EventParticipationExportRowDto>> GetEventParticipationRowsAsync(int eventId, CancellationToken cancellationToken = default);
}
