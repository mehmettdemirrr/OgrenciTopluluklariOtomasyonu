using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-01/A-30/Y-38: tek kullanımlık, çerezde taşınan refresh token.
/// Ham token değeri asla saklanmaz — yalnızca hash'i (Business, IClock ile süre/rotasyon kararı verir).
/// </summary>
public sealed class RefreshToken : IEntity
{
    public int Id { get; set; }

    public int ApplicationUserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public int? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }
}
