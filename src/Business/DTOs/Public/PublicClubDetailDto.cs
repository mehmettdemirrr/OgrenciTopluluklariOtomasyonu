using Business.DTOs.Clubs;

namespace Business.DTOs.Public;

/// <summary>docs/PLAN-V2.md · Faz 14 (Y-58): AdvisorId/CreatedAtUtc gibi iç alanlar kasıtlı olarak yok.</summary>
public sealed class PublicClubDetailDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? LogoFileId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-35: kategori adı. Y-58 kontrolü: kurumsal sınıflandırma, kişisel veri değil.
    /// Kimlik (ClubCategoryId) taşınmaz — vitrinin ihtiyacı yalnızca görünen ad.
    /// </summary>
    /// <summary>docs/MIMARI.md · K-49: ada göre sıralı kategori adları. Y-58: kişisel veri değil.</summary>
    public IReadOnlyList<string> ClubCategoryNames { get; set; } = [];

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim e-postası. Null = tanımsız. Y-58: kurumsal iletişim, kişisel veri değil.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim telefonu. Null = tanımsız.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>docs/MIMARI.md · K-44/A-74: DisplayOrder'a göre sıralı sosyal bağlantılar.</summary>
    public IReadOnlyList<ClubSocialLinkDto> SocialLinks { get; set; } = [];
}
