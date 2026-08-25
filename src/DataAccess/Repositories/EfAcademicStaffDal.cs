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
            where term.Length == 0
                || user.Email!.Contains(term)
                || staff.Title.Contains(term)
                || (user.FirstName != null && user.FirstName.Contains(term))
                || (user.LastName != null && user.LastName.Contains(term))
            // Y-64: e-posta benzersiz olduğundan tek başına deterministik sıra sağlar.
            orderby user.Email
            select new AcademicStaffRowDto
            {
                Id = staff.Id,
                Title = staff.Title,
                Email = user.Email!,
                // A-56: ad soyad geldi; seçicide artık e-posta yerine bu gösterilir.
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<AcademicStaffRowDto>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<Dictionary<int, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await (
                from staff in context.AcademicStaff.AsNoTracking()
                join user in context.Users.AsNoTracking() on staff.ApplicationUserId equals user.Id
                where ids.Contains(staff.Id)
                select new { staff.Id, staff.Title, user.FirstName, user.LastName, user.Email })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Birleştirme bellekte: ad soyadın nasıl birleşeceği bir sunum kuralıdır, SQL'e taşımanın
        // (ve sağlayıcıya göre değişen NULL/boşluk davranışına güvenmenin) bir kazancı yok.
        return rows.ToDictionary(
            r => r.Id,
            r =>
            {
                var fullName = $"{r.FirstName} {r.LastName}".Trim();
                return string.IsNullOrEmpty(fullName) ? $"{r.Title} ({r.Email})" : $"{r.Title} {fullName}";
            });
    }
}
