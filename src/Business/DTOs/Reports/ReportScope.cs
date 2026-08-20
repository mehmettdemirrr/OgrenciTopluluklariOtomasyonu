namespace Business.DTOs.Reports;

/// <summary>docs/MIMARI.md · Y-51: bir kullanıcının rapor için kapsamı — token'dan değil DB'den çözülür.</summary>
public sealed record ReportScope(bool AllClubs, IReadOnlyCollection<int> ClubIds)
{
    public static readonly ReportScope None = new(false, []);

    public bool Covers(int clubId) => AllClubs || ClubIds.Contains(clubId);
}
