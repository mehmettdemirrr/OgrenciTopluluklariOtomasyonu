namespace Business.DTOs.Clubs;

/// <summary>docs/MIMARI.md · K-44/A-74: iletişim bilgileri ve sosyal bağlantılar topluca değiştirilir.</summary>
public sealed class SetClubSocialLinksRequestDto
{
    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public IReadOnlyList<ClubSocialLinkDto> Links { get; set; } = [];
}
