namespace Business.DTOs.Clubs;

public sealed class ClubDetailDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int AdvisorId { get; set; }

    public int? LogoFileId { get; set; }

    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }

    /// <summary>
    /// Kategori adı. Y-32: AutoMapper doldurmaz (Club üzerinde kaynağı yok, join gerekir) —
    /// profilde Ignore edilir, ClubManager kategori sözlüğünden doldurur.
    /// </summary>
    public string? ClubCategoryName { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim e-postası. Null = tanımsız.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim telefonu. Null = tanımsız.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>docs/MIMARI.md · K-44/A-74: DisplayOrder'a göre sıralı sosyal bağlantılar.</summary>
    public IReadOnlyList<ClubSocialLinkDto> SocialLinks { get; set; } = [];
}
