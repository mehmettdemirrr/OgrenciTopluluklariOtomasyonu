using Core.Entities;

namespace Entities;

/// <summary>docs/MIMARI.md · K-49/A-80: topluluk–kategori bağı. Kulüp başına en fazla 3 satır (validator).</summary>
public sealed class ClubCategoryAssignment : IEntity
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public int ClubCategoryId { get; set; }
}
