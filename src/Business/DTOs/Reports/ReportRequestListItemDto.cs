using Entities.Enums;

namespace Business.DTOs.Reports;

public sealed class ReportRequestListItemDto
{
    public int Id { get; set; }

    public ReportType ReportType { get; set; }

    public ReportStatus Status { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public bool HasFile { get; set; }

    public string? ErrorMessage { get; set; }
}
