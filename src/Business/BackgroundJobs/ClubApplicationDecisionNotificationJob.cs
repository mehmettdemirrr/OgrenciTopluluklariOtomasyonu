using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Email;
using Entities;
using Entities.Enums;

namespace Business.BackgroundJobs;

/// <summary>
/// docs/MIMARI.md · Y-47: yalnızca kimlik (id) alır; MembershipDecisionNotificationJob'ın kalıbı.
/// Hiç yazma yapmaz — bulunamama/hâlâ Pending olma durumunda sessizce çıkar, idempotent.
/// </summary>
public sealed class ClubApplicationDecisionNotificationJob(
    IEntityRepository<ClubApplication> clubApplicationRepository,
    IEntityRepository<Student> studentRepository,
    IIdentityGateway identityGateway,
    IEmailSender emailSender)
{
    public async Task SendAsync(int clubApplicationId)
    {
        var application = await clubApplicationRepository
            .GetAsync(a => a.Id == clubApplicationId)
            .ConfigureAwait(false);

        if (application is null || application.Status == ApplicationStatus.Pending)
        {
            return;
        }

        var student = await studentRepository.GetAsync(s => s.Id == application.StudentId).ConfigureAwait(false);
        if (student is null)
        {
            return;
        }

        var studentUser = await identityGateway.FindByIdAsync(student.ApplicationUserId).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(studentUser?.Email))
        {
            return;
        }

        var decision = application.Status == ApplicationStatus.Approved ? "onaylandı" : "reddedildi";
        var subject = $"\"{application.ProposedName}\" topluluk kurma başvurunuz {decision}";
        var approvedNote = application.Status == ApplicationStatus.Approved
            ? "\n\nArtık bu topluluğun başkanısınız."
            : string.Empty;
        var reviewNote = string.IsNullOrWhiteSpace(application.ReviewNote) ? string.Empty : $"\n\nNot: {application.ReviewNote}";
        var body = $"Merhaba,\n\n\"{application.ProposedName}\" adıyla yaptığınız topluluk kurma başvurunuz {decision}.{approvedNote}{reviewNote}\n\nİyi çalışmalar.";

        await emailSender.SendAsync(studentUser.Email, subject, body).ConfigureAwait(false);
    }
}
