using Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-85: her metot tek sorgudur; satır başına sorgu yok.</summary>
public sealed class EfClubCategoryAssignmentDal(AppDbContext context) : IClubCategoryAssignmentDal
{
    public async Task<IReadOnlyCollection<int>> GetClubIdsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        await context.ClubCategoryAssignments.AsNoTracking()
            .Where(a => a.ClubCategoryId == categoryId)
            .Select(a => a.ClubId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetNamesByClubAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default)
    {
        if (clubIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<string>>();
        }

        var rows = await context.ClubCategoryAssignments.AsNoTracking()
            .Where(a => clubIds.Contains(a.ClubId))
            .Join(context.ClubCategories.AsNoTracking(), a => a.ClubCategoryId, c => c.Id, (a, c) => new { a.ClubId, c.Name })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(x => x.ClubId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).ToList());
    }

    public async Task ReplaceAsync(int clubId, IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken = default)
    {
        var existing = await context.ClubCategoryAssignments
            .Where(a => a.ClubId == clubId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        context.ClubCategoryAssignments.RemoveRange(existing);

        foreach (var categoryId in categoryIds.Distinct())
        {
            await context.ClubCategoryAssignments
                .AddAsync(new ClubCategoryAssignment { ClubId = clubId, ClubCategoryId = categoryId }, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
