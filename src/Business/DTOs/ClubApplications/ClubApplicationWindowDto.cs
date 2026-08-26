using Entities.Enums;

namespace Business.DTOs.ClubApplications;

/// <summary>
/// docs/MIMARI.md · K-39/A-66/Y-73: başvuru penceresinin durumu. <c>IsOpen</c> kararı sunucuda
/// verilir — arayüz tarihlere bakıp kendi kararını vermez (Y-35). Tarihler yalnızca
/// "3 Ekim'de açılıyor" metnini kurmak için taşınır.
/// </summary>
public sealed class ClubApplicationWindowDto
{
    public bool IsOpen { get; set; }

    public DateTime? StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    public ClubApplicationWindowOverride Override { get; set; }

    /// <summary>Güncel dönem yoksa boş string.</summary>
    public required string TermName { get; set; }
}
