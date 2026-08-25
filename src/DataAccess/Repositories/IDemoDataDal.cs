namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · Y-68/Y-08: demo verisinin geri alınması. Generic repository ile yapılamaz,
/// çünkü iki şey gerekiyor: <c>IgnoreQueryFilters</c> (soft delete edilmiş demo satırları da
/// silinmeli, aksi hâlde ebeveynleri FK kısıtına takılır) ve <b>hard delete</b> (Y-16 soft
/// delete demo verisini geri almaz, yalnızca gizler). İkisi de EF'e ait; bu yüzden DAL'da.
/// </summary>
public interface IDemoDataDal
{
    /// <summary>
    /// Künye tablosunda işaretli tüm domain satırlarını çocuktan ebeveyne doğru siler ve
    /// künyeleri temizler. Kullanıcı satırlarına <b>dokunmaz</b> — Identity'nin bağlı tablolarını
    /// (rol, claim, token) doğru boşaltmak UserManager'ın işi. Silinmesi gereken kullanıcı
    /// kimliklerini döndürür.
    /// </summary>
    Task<IReadOnlyCollection<int>> PurgeAsync(CancellationToken cancellationToken = default);
}
