using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Email;
using Entities;
using Entities.Enums;
using Serilog;

namespace Business.BackgroundJobs;

/// <summary>
/// docs/MIMARI.md · A-49/Y-47: iptal edilen etkinliğin **tüm** kayıtlı katılımcılarına bildirim.
/// Diğer bildirim işlerinden farkı fan-out olması — tek kişiye değil, katılımcı listesine gönderir.
/// Yalnızca `eventId` alır, kendi scope'unu açar, hiç yazma yapmaz (idempotent: yeniden çalışırsa
/// yalnızca e-postalar tekrarlanır, veri bozulmaz).
///
/// **Ölçek sınırı:** kontenjanı 300 olan bir etkinliğin iptali 300 e-posta demektir; Gmail'in 500/gün
/// sınırında tek iptal günlük kotanın yarısını yer. SmtpException yutulmaz (Y-47) — kota aşılırsa iş
/// Failed olur ve Hangfire panelinde görünür, sessizce kaybolmaz.
/// </summary>
public sealed class EventCancellationNotificationJob(
    IEntityRepository<Event> eventRepository,
    IEntityRepository<EventParticipation> eventParticipationRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<Club> clubRepository,
    IIdentityGateway identityGateway,
    IEmailSender emailSender)
{
    public async Task SendAsync(int eventId)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId).ConfigureAwait(false);
        if (@event is null || @event.Status != EventStatus.Cancelled)
        {
            return;
        }

        var participations = await eventParticipationRepository
            .GetListAsync(p => p.EventId == eventId)
            .ConfigureAwait(false);

        if (participations.Count == 0)
        {
            return;
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId).ConfigureAwait(false);
        var clubName = club?.Name ?? "topluluk";

        var studentIds = participations.Select(p => p.StudentId).Distinct().ToList();
        var students = await studentRepository.GetListAsync(s => studentIds.Contains(s.Id)).ConfigureAwait(false);

        var subject = $"\"{@event.Title}\" etkinliği iptal edildi";
        var reason = string.IsNullOrWhiteSpace(@event.CancellationReason)
            ? string.Empty
            : $"\n\nİptal gerekçesi: {@event.CancellationReason}";
        var body = $"Merhaba,\n\n{clubName} topluluğunun {@event.StartDateUtc:dd.MM.yyyy HH:mm} tarihli "
            + $"\"{@event.Title}\" etkinliği iptal edilmiştir.{reason}\n\nİyi çalışmalar.";

        foreach (var student in students)
        {
            var user = await identityGateway.FindByIdAsync(student.ApplicationUserId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(user?.Email))
            {
                // Tek bir katılımcının e-postası yoksa fan-out'un tamamı durmamalı.
                Log.Warning("Etkinlik iptal bildirimi gönderilemedi: öğrenci {StudentId} için e-posta yok.", student.Id);
                continue;
            }

            await emailSender.SendAsync(user.Email, subject, body).ConfigureAwait(false);
        }
    }
}
