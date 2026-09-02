namespace Business.DTOs.Public;

/// <summary>
/// docs/PLAN-V2.md · Faz 14 (A-42/Y-58): vitrin kategori filtresi. Yetkili ClubCategoryListItemDto
/// yeniden kullanılmaz. Id sınıflandırma kimliğidir (kişisel veri değil); filtre query'si buna bağlanır.
/// </summary>
public sealed class PublicClubCategoryDto
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
