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
}
