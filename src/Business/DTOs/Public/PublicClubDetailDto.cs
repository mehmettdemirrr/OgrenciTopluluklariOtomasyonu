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
    public string? ClubCategoryName { get; set; }
}
