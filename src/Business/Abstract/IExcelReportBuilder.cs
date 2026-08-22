using Entities.Dtos.Reports;

namespace Business.Abstract;

/// <summary>docs/MIMARI.md · K-06: raporun ClosedXML ile üretilen ham .xlsx baytları. Aspect taşımaz.</summary>
public interface IExcelReportBuilder
{
    byte[] BuildClubMemberWorkbook(IReadOnlyList<ClubMemberExportRowDto> rows, string clubName, string termName);

    byte[] BuildEventParticipationWorkbook(IReadOnlyList<EventParticipationExportRowDto> rows, string eventTitle);

    byte[] BuildTermSummaryWorkbook(IReadOnlyList<TermSummaryRowDto> rows);
}
