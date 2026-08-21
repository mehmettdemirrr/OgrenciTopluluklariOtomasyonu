using Core.DataAccess;
using Entities.Dtos.Reference;

namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · Y-08: AcademicStaff'ın e-postası Identity'de yaşar (ApplicationUser) — bu DAL
/// ikisini join'leyip materyalize sonuç döner; Business, Identity DbContext'ine hiç dokunmaz.
/// </summary>
public interface IAcademicStaffDal
{
    Task<PagedResult<AcademicStaffRowDto>> GetListPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
