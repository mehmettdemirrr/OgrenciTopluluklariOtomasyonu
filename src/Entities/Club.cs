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

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
