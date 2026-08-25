using Core.DataAccess;
using Entities.Dtos.Reference;

namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · Y-08: AcademicStaff'ın e-postası Identity'de yaşar (ApplicationUser) — bu DAL
/// ikisini join'leyip materyalize sonuç döner; Business, Identity DbContext'ine hiç dokunmaz.
/// </summary>
public interface IAcademicStaffDal
{
    /// <summary>A-50: `search` boş değilse e-posta ve unvan üzerinde SQL tarafında filtreler.</summary>
    Task<PagedResult<AcademicStaffRowDto>> GetListPagedAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · A-56: danışmanın ekranda görünecek adı — "Prof. Dr. Elif Yıldırım".
    /// Ad soyad Identity'de (ApplicationUser) yaşadığı için Business bunu tek başına kuramaz;
    /// join burada yapılır. Adı olmayan hesap için unvan + e-postaya düşülür.
    /// </summary>
    Task<Dictionary<int, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
