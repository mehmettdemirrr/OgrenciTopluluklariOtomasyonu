using Entities.Dtos.Dashboard;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-42: toplama SQL'de; hiçbir yerde ToList() sonrası bellekte gruplama yok.</summary>
public sealed class EfDashboardDal(AppDbContext context) : IDashboardDal
{
    public async Task<PersonalDashboardStatsDto> GetPersonalStatsAsync(int studentId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var clubCount = await context.ClubMemberships.AsNoTracking()
            .Where(m => m.StudentId == studentId)
            .Select(m => m.ClubId)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var pendingApplicationCount = await context.MembershipApplications.AsNoTracking()
            .CountAsync(a => a.StudentId == studentId && a.Status == ApplicationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        var upcomingEventCount = await context.EventParticipations.AsNoTracking()
            .Join(context.Events.AsNoTracking(), p => p.EventId, e => e.Id, (p, e) => new { p, e })
            .CountAsync(x => x.p.StudentId == studentId && x.e.StartDateUtc >= nowUtc, cancellationToken)
            .ConfigureAwait(false);

        return new PersonalDashboardStatsDto
        {
            MyClubCount = clubCount,
            MyPendingApplicationCount = pendingApplicationCount,
            MyUpcomingEventCount = upcomingEventCount,
        };
    }

    public async Task<ManagementDashboardStatsDto> GetManagementStatsAsync(
        bool allClubs, IReadOnlyCollection<int> clubIds, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var memberships = context.ClubMemberships.AsNoTracking().AsQueryable();
        var applications = context.MembershipApplications.AsNoTracking().AsQueryable();
        var events = context.Events.AsNoTracking().AsQueryable();

        if (!allClubs)
        {
            memberships = memberships.Where(m => clubIds.Contains(m.ClubId));
            applications = applications.Where(a => clubIds.Contains(a.ClubId));
            events = events.Where(e => clubIds.Contains(e.ClubId));
        }

        var scopeClubCount = allClubs
            ? await context.Clubs.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken).ConfigureAwait(false)
            : clubIds.Count;

        var scopeMemberCount = await memberships.CountAsync(cancellationToken).ConfigureAwait(false);
        var scopePendingApplicationCount = await applications
            .CountAsync(a => a.Status == ApplicationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);
        var scopeUpcomingEventCount = await events
            .CountAsync(e => e.Status == EventStatus.Published && e.StartDateUtc >= nowUtc, cancellationToken)
            .ConfigureAwait(false);

        return new ManagementDashboardStatsDto
        {
            AllClubs = allClubs,
            ScopeClubCount = scopeClubCount,
            ScopeMemberCount = scopeMemberCount,
            ScopePendingApplicationCount = scopePendingApplicationCount,
            ScopeUpcomingEventCount = scopeUpcomingEventCount,
        };
    }
}
