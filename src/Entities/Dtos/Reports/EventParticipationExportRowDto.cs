namespace Entities.Dtos.Reports;

/// <summary>docs/MIMARI.md · K-06: etkinlik katılım listesi Excel çıktısının bir satırı.</summary>
public sealed class EventParticipationExportRowDto
{
    public required string StudentNumber { get; set; }

    public required string Email { get; set; }

    public required string EventTitle { get; set; }

    public DateTime RegisteredAtUtc { get; set; }
}
