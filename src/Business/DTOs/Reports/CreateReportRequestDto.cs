namespace Business.DTOs.Reports;

public sealed class CreateReportRequestDto
{
    public ReportType ReportType { get; set; }

    public int? ClubId { get; set; }

    public int? EventId { get; set; }
}
