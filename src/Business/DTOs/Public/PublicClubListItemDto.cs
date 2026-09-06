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
}
