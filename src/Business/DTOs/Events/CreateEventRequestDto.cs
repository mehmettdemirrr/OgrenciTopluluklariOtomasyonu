using Entities.Enums;

namespace Business.DTOs.Events;

public sealed class CreateEventRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? Capacity { get; set; }

    /// <summary>docs/MIMARI.md · K-38/A-65: katılım kitlesi. Verilmezse Public (enum varsayılanı).</summary>
    public EventAudience Audience { get; set; }
}
