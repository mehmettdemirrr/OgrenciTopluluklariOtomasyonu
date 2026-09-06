using Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-81: iki GROUP BY sorgusu (üyelik + etkinlik), satır başına sayım yok.</summary>
public sealed class EfClubStatsDal(AppDbContext context) : IClubStatsDal
{
    public async Task<IReadOnlyDictionary<int, ClubCardStats>> GetCountsAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default)
    {
        if (clubIds.Count == 0)
        {
            return new Dictionary<int, ClubCardStats>();
        }

        // §22.3: üyelik dönemseldir — güncel dönem yoksa üye sayısı 0'dır, geçmiş dönem toplanmaz.
        var currentTermId = await context.AcademicTerms.AsNoTracking()
            .Where(t => t.IsCurrent)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var memberCounts = currentTermId is { } termId
            ? await context.ClubMemberships.AsNoTracking()
                .Where(m => clubIds.Contains(m.ClubId) && m.AcademicTermId == termId)
                .GroupBy(m => m.ClubId)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count, cancellationToken)
                .ConfigureAwait(false)
            : [];

        // Y-58/Y-72: vitrinde görünmeyen etkinlik sayılmaz.
        var eventCounts = await context.Events.AsNoTracking()
            .Where(e => clubIds.Contains(e.ClubId) && e.Status == EventStatus.Published && e.Audience == EventAudience.Public)
            .GroupBy(e => e.ClubId)
            .Select(g => new { ClubId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClubId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return clubIds.ToDictionary(
            id => id,
            id => new ClubCardStats(memberCounts.GetValueOrDefault(id), eventCounts.GetValueOrDefault(id)));
    }
}
