using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-35/A-60: topluluk kategorisi. Faculty/Department ile aynı sınıf referans
/// verisi — hard delete serbest (A-12), kullanımdaysa FK Restrict 409 üretir.
/// </summary>
public sealed class ClubCategory : IEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
