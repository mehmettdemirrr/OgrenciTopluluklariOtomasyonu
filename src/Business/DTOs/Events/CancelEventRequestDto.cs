namespace Business.DTOs.Events;

/// <summary>docs/MIMARI.md · A-49: iptal gerekçesi zorunlu — katılımcılara giden e-postada yer alır.</summary>
public sealed class CancelEventRequestDto
{
    public string CancellationReason { get; set; } = string.Empty;
}
