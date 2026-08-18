namespace Entities.Enums;

/// <summary>docs/MIMARI.md · K-06 / A-35: rapor talebi durum makinesi.</summary>
public enum ReportStatus
{
    Queued = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3,
}
