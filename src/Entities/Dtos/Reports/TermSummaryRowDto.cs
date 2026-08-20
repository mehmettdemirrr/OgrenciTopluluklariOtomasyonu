namespace Entities.Dtos.Reports;

/// <summary>
/// docs/MIMARI.md · Y-42: DataAccess'in aggregate rapor sorgusundan doğrudan projekte ettiği satır.
/// Davranışsız düz DTO; DataAccess yalnızca Entities+Core'a referans verebildiği için burada yaşar.
/// </summary>
public sealed class TermSummaryRowDto
{
    public int AcademicTermId { get; set; }

    public required string TermName { get; set; }

    public int ClubCount { get; set; }

    public int MemberCount { get; set; }

    public int EventCount { get; set; }
}
