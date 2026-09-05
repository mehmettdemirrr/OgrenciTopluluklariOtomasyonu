using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>docs/MIMARI.md · K-44/A-74: topluluğun sosyal medya bağlantısı. Y-80: yalnızca https.</summary>
public sealed class ClubSocialLink : IEntity
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public SocialPlatform Platform { get; set; }

    public required string Url { get; set; }

    public int DisplayOrder { get; set; }
}
