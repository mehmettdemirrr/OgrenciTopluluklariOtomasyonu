using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-06/A-35/Y-51: rapor talebi durum makinesi.
/// Kuyrukta → üretiliyor → hazır/hatalı. Üretilen dosya StoredFile olarak saklanır.
/// </summary>
public sealed class ReportRequest : IEntity
{
    public int Id { get; set; }

    public int RequestedByUserId { get; set; }

    public required string ReportType { get; set; }

    public string? ParametersJson { get; set; }

    public ReportStatus Status { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int? OutputFileId { get; set; }

    public string? ErrorMessage { get; set; }
}
