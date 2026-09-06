namespace Business.DTOs.Public;

/// <summary>docs/PLAN-V2.md · Faz 14 (A-42/Y-58): ayrı DTO ailesi — yetkili ClubListItemDto asla yeniden kullanılmaz.</summary>
public sealed class PublicClubListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? LogoFileId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-49: ada göre sıralı kategori adları. Y-58 kontrolü: kurumsal
    /// sınıflandırma, kişisel veri değil. Kimlik taşınmaz — vitrinin ihtiyacı yalnızca görünen ad.
    /// </summary>
    public IReadOnlyList<string> ClubCategoryNames { get; set; } = [];

    /// <summary>docs/MIMARI.md · A-81: güncel dönemin üye sayısı. İsim/öğrenci no taşınmaz (Y-58).</summary>
    public int MemberCount { get; set; }

    /// <summary>docs/MIMARI.md · A-81: vitrinde görünen (yayınlanmış + herkese açık) etkinlik sayısı.</summary>
    public int EventCount { get; set; }
}
