using Core.DataAccess;
using Entities.Dtos.Reference;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class EfAcademicStaffDal(AppDbContext context) : IAcademicStaffDal
{
    public async Task<PagedResult<AcademicStaffRowDto>> GetListPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query =
            from staff in context.AcademicStaff.AsNoTracking()
            join user in context.Users.AsNoTracking() on staff.ApplicationUserId equals user.Id
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
