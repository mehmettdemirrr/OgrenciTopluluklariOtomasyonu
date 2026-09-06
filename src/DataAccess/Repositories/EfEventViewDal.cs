using Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-77/Y-82: filtre burada da uygulanır — gizli etkinliğin sayacı artmaz.</summary>
public sealed class EfEventViewDal(AppDbContext context) : IEventViewDal
{
    public async Task<int> IncrementAsync(int eventId, CancellationToken cancellationToken = default) =>
        await context.Events
            .Where(e => e.Id == eventId && e.Status == EventStatus.Published && e.Audience == EventAudience.Public)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.ViewCount, e => e.ViewCount + 1), cancellationToken)
            .ConfigureAwait(false);
}
