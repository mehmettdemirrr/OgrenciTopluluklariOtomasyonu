namespace Business.DTOs.Reports;

/// <summary>docs/MIMARI.md · K-06: rapor türleri. ReportRequest.ReportType alanında string olarak saklanır.</summary>
public enum ReportType
{
    ClubMembers = 0,
    EventParticipants = 1,

    /// <summary>docs/PLAN-V2.md · Faz 13: ClubId/EventId gerektirmez — kapsam yalnızca IReportScopeResolver'dan gelir.</summary>
    TermSummary = 2,
}
