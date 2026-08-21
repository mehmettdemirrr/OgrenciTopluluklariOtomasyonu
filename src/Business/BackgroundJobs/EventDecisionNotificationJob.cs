using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Email;
using Entities;
using Entities.Enums;

namespace Business.BackgroundJobs;

/// <summary>
/// docs/MIMARI.md · Y-47: yalnızca kimlik (id) alır, entity/DbContext/servis örneği geçilmez;
/// kendi scope'unu DI ile açar. Hiç yazma yapmaz — Hangfire'ın olası tekrar denemesi güvenlidir (idempotent).
/// docs/PLAN-V2.md §10.5: Event, kim tarafından oluşturulduğunu (CreatedByUserId) tutmuyor — en yakın
/// "sahip" temsilcisi güncel dönemde kulübün Officer/President üyelikleridir, bildirim onlara gider.
/// </summary>
public sealed class EventDecisionNotificationJob(
    IEntityRepository<Event> eventRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IEntityRepository<Student> studentRepository,
    IIdentityGateway identityGateway,
    IEmailSender emailSender)
{
    public async Task SendAsync(int eventId)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId).ConfigureAwait(false);
        if (@event is null || @event.Status is not (EventStatus.Published or EventStatus.Rejected))
        {
            return;
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent).ConfigureAwait(false);
        if (term is null)
        {
            return;
        }

        var studentIds = (await clubMembershipRepository
                .GetListAsync(m => m.ClubId == @event.ClubId && m.AcademicTermId == term.Id && (m.ClubRole == ClubRole.Officer || m.ClubRole == ClubRole.President))
                .ConfigureAwait(false))
            .Select(m => m.StudentId)
            .Distinct()
            .ToList();

        if (studentIds.Count == 0)
        {
            return;
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId).ConfigureAwait(false);
        var clubName = club?.Name ?? "topluluk";
        var decision = @event.Status == EventStatus.Published ? "yayınlandı" : "reddedildi";
        var subject = $"{clubName} — \"{@event.Title}\" etkinliği {decision}";
        var body = $"Merhaba,\n\n{clubName} topluluğunda \"{@event.Title}\" etkinliği {decision}.\n\nİyi çalışmalar.";

        var students = await studentRepository.GetListAsync(s => studentIds.Contains(s.Id)).ConfigureAwait(false);
        foreach (var student in students)
        {
            var user = await identityGateway.FindByIdAsync(student.ApplicationUserId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(user?.Email))
            {
                continue;
            }

            await emailSender.SendAsync(user.Email, subject, body).ConfigureAwait(false);
        }
    }
}
