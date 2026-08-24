using Core.DataAccess;
using Entities.Dtos.Reference;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class EfAcademicStaffDal(AppDbContext context) : IAcademicStaffDal
{
    public async Task<PagedResult<AcademicStaffRowDto>> GetListPagedAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var term = search?.Trim() ?? string.Empty;

        var query =
            from staff in context.AcademicStaff.AsNoTracking()
            join user in context.Users.AsNoTracking() on staff.ApplicationUserId equals user.Id
            where term.Length == 0 || user.Email!.Contains(term) || staff.Title.Contains(term)
            // Y-64: e-posta benzersiz olduğundan tek başına deterministik sıra sağlar.
            orderby user.Email
            select new AcademicStaffRowDto { Id = staff.Id, Title = staff.Title, Email = user.Email! };

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<AcademicStaffRowDto>(items, totalCount, pageIndex, pageSize);
    }
}
