using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-58: pasif kulübün sayacı artmaz — vitrin filtresi burada da uygulanır.</summary>
public sealed class EfClubViewDal(AppDbContext context) : IClubViewDal
{
    public async Task<int> IncrementAsync(int clubId, CancellationToken cancellationToken = default) =>
        await context.Clubs
            .Where(c => c.Id == clubId && c.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.ViewCount, c => c.ViewCount + 1), cancellationToken)
            .ConfigureAwait(false);
}
