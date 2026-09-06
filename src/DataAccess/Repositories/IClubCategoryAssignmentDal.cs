namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · A-80/Y-85: kategori bağının join gerektiren okumaları. Bu kod tabanında
/// navigation property yok; join'ler burada, SQL tarafında yapılır (IDashboardDal precedent'i).
/// </summary>
public interface IClubCategoryAssignmentDal
{
    /// <summary>Filtre için: bu kategoriye bağlı kulüp id'leri — tek sorgu.</summary>
    Task<IReadOnlyCollection<int>> GetClubIdsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>Gösterim için: verilen kulüplerin kategori adları, ada göre sıralı — tek sorgu.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetNamesByClubAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default);

    /// <summary>Kulübün kategorilerini topluca değiştirir (eskiler silinir, yeniler yazılır). SaveChanges çağıran katmana aittir.</summary>
    Task ReplaceAsync(int clubId, IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken = default);
}
