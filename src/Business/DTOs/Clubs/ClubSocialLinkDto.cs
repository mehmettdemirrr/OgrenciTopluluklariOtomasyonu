using Entities.Enums;

namespace Business.DTOs.Clubs;

/// <summary>docs/MIMARI.md · K-44/A-74: bir sosyal medya bağlantısı.</summary>
public sealed class ClubSocialLinkDto
{
    public SocialPlatform Platform { get; set; }

    public required string Url { get; set; }

    public int DisplayOrder { get; set; }
}
