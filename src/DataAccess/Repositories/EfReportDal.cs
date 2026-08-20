using Entities.Dtos.Reports;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-42: toplama SQL'de; hiçbir yerde ToList() sonrası bellekte gruplama yok.</summary>
public sealed class EfReportDal(AppDbContext context) : IReportDal
{
    public Task<List<TermSummaryRowDto>> GetTermSummaryAsync(IReadOnlyCollection<int>? clubIdScope, CancellationToken cancellationToken = default)
    {
        var memberships = context.ClubMemberships.AsNoTracking().AsQueryable();
        var events = context.Events.AsNoTracking().AsQueryable();

        if (clubIdScope is not null)
        {
            memberships = memberships.Where(m => clubIdScope.Contains(m.ClubId));
            events = events.Where(e => clubIdScope.Contains(e.ClubId));
        }

        // Event'te dönem FK'sı yok — tarih aralığıyla eşleştirilir (soft-delete query filter'ları
        // ClubMembership/Event üzerinde otomatik devrede).
        return context.AcademicTerms
            .AsNoTracking()
            .OrderByDescending(term => term.Id)
            .Select(term => new TermSummaryRowDto
            {
                AcademicTermId = term.Id,
                TermName = term.Name,
                ClubCount = memberships.Where(m => m.AcademicTermId == term.Id).Select(m => m.ClubId).Distinct().Count(),
                MemberCount = memberships.Count(m => m.AcademicTermId == term.Id),
                EventCount = events.Count(e => e.StartDateUtc >= term.StartDateUtc && e.StartDateUtc <= term.EndDateUtc),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<ClubMemberExportRowDto>> GetClubMemberRowsAsync(int clubId, int academicTermId, CancellationToken cancellationToken = default) =>
        context.ClubMemberships
            .AsNoTracking()
            .Where(m => m.ClubId == clubId && m.AcademicTermId == academicTermId)
            .Join(context.Students.AsNoTracking(), m => m.StudentId, s => s.Id, (m, s) => new { m, s })
            .Join(context.Users.AsNoTracking(), x => x.s.ApplicationUserId, u => u.Id, (x, u) => new { x.m, x.s, u })
            .Join(context.Departments.AsNoTracking(), x => x.s.DepartmentId, d => d.Id, (x, d) => new ClubMemberExportRowDto
            {
                StudentNumber = x.s.StudentNumber,
                Email = x.u.Email!,
                DepartmentName = d.Name,
                ClubRole = x.m.ClubRole,
                JoinedAtUtc = x.m.JoinedAtUtc,
            })
            .ToListAsync(cancellationToken);

    public Task<List<EventParticipationExportRowDto>> GetEventParticipationRowsAsync(int eventId, CancellationToken cancellationToken = default) =>
        context.EventParticipations
            .AsNoTracking()
            .Where(p => p.EventId == eventId)
            .Join(context.Students.AsNoTracking(), p => p.StudentId, s => s.Id, (p, s) => new { p, s })
            .Join(context.Users.AsNoTracking(), x => x.s.ApplicationUserId, u => u.Id, (x, u) => new { x.p, x.s, u })
            .Join(context.Events.AsNoTracking(), x => x.p.EventId, e => e.Id, (x, e) => new EventParticipationExportRowDto
            {
                StudentNumber = x.s.StudentNumber,
                Email = x.u.Email!,
                EventTitle = e.Title,
                RegisteredAtUtc = x.p.RegisteredAtUtc,
            })
            .ToListAsync(cancellationToken);
}
