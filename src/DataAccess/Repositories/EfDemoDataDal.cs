using Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · Y-68: yalnızca künyeli satırlar silinir. Hiçbir yerde ad kalıbına ya da
/// tarihe bakan toplu silme yoktur — üretim verisine dokunmanın tek yolu budur ve kapalıdır.
/// </summary>
public sealed class EfDemoDataDal(AppDbContext context) : IDemoDataDal
{
    public async Task<IReadOnlyCollection<int>> PurgeAsync(CancellationToken cancellationToken = default)
    {
        var records = await context.DemoSeedRecords.AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return [];
        }

        var idsByType = records
            .GroupBy(r => r.EntityType)
            .ToDictionary(g => g.Key, g => g.Select(r => r.EntityId).ToList());

        foreach (var entityType in DemoEntityTypes.DeletionOrder())
        {
            if (entityType == DemoEntityTypes.ApplicationUser || !idsByType.TryGetValue(entityType, out var ids))
            {
                continue;
            }

            await DeleteByIdsAsync(entityType, ids, cancellationToken).ConfigureAwait(false);
        }

        await context.DemoSeedRecords
            .Where(r => r.EntityType != DemoEntityTypes.ApplicationUser)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return idsByType.GetValueOrDefault(DemoEntityTypes.ApplicationUser) ?? [];
    }

    /// <summary>
    /// IgnoreQueryFilters şart: soft delete edilmiş bir demo etkinliği silinmeden kulübü
    /// silinemez (FK Restrict). ExecuteDelete tek SQL cümlesi üretir — Y-42.
    /// </summary>
    private Task DeleteByIdsAsync(string entityType, List<int> ids, CancellationToken cancellationToken) => entityType switch
    {
        DemoEntityTypes.EventParticipation => context.EventParticipations.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.Announcement => context.Announcements.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.Event => context.Events.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.MembershipApplication => context.MembershipApplications.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.ClubApplication => context.ClubApplications.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.ClubMembership => context.ClubMemberships.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.Club => context.Clubs.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.AcademicStaff => context.AcademicStaff.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        DemoEntityTypes.Student => context.Students.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken),
        _ => throw new InvalidOperationException($"Bilinmeyen demo entity türü: {entityType}"),
    };
}
