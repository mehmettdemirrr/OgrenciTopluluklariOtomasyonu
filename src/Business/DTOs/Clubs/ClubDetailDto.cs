using Entities.Enums;

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

    /// <summary>
    /// docs/MIMARI.md · K-49/A-80: ada göre sıralı kategori adları. Boş liste = kategorisiz.
    /// Y-32: AutoMapper doldurmaz (Club üzerinde kaynağı yok, join gerekir) — profilde Ignore
    /// edilir, ClubManager tek toplu sorgudan doldurur.
    /// </summary>
    public IReadOnlyList<string> ClubCategoryNames { get; set; } = [];

    /// <summary>docs/MIMARI.md · K-49: düzenleme formu bu kimliklerle kutucukları işaretler.</summary>
    public IReadOnlyList<int> ClubCategoryIds { get; set; } = [];

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim e-postası. Null = tanımsız.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim telefonu. Null = tanımsız.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>docs/MIMARI.md · K-51/Y-87: kuruluş yılı. Null = bilinmiyor.</summary>
    public int? FoundedYear { get; set; }

    /// <summary>docs/MIMARI.md · A-82: yaklaşık görüntülenme sayısı.</summary>
    public int ViewCount { get; set; }

    /// <summary>docs/MIMARI.md · K-44/A-74: DisplayOrder'a göre sıralı sosyal bağlantılar.</summary>
    public IReadOnlyList<ClubSocialLinkDto> SocialLinks { get; set; } = [];

    /// <summary>docs/MIMARI.md · A-75: arayüz sekmeleri bu alandan çizilir, global izinden değil.</summary>
    public ClubRelationship MyRelationship { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-68/A-75: çağıranın BU kulüpteki kapasiteleri.
    /// [Flags] olduğu için tel üzerinde SAYI gider — Program.cs'teki JsonNumberEnumConverter&lt;ClubCapability&gt;
    /// kaydı bunu sağlar; string'e dönerse arayüzün bit maskesi çalışmaz.
    /// </summary>
    public ClubCapability MyCapabilities { get; set; }
}
