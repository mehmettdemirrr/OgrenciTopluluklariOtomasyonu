using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-05/A-31: topluluk, danışmanı, logosu, durumu.
/// A-15: rowversion ile eşzamanlılık kontrolü.
/// </summary>
public sealed class Club : IEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int AdvisorId { get; set; }

    public int? LogoFileId { get; set; }

    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Nullable — mevcut kulüpler kategorisiz kalabilir.</summary>
    public int? ClubCategoryId { get; set; }

    public bool IsActive { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim e-postası. Null = tanımsız.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim telefonu. Null = tanımsız.</summary>
    public string? ContactPhone { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
