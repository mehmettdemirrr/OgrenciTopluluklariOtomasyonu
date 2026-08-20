using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Email;
using Entities;
using Entities.Enums;

namespace Business.BackgroundJobs;

/// <summary>
/// docs/MIMARI.md · Y-47: yalnızca kimlik (id) alır, entity/DbContext/servis örneği geçilmez;
/// kendi scope'unu DI ile açar. Hiç yazma yapmaz — bulunamama/hâlâ Pending olma durumunda
/// sessizce çıkar, bu yüzden Hangfire'ın olası tekrar denemesi güvenlidir (idempotent).
/// </summary>
public sealed class MembershipDecisionNotificationJob(
    IEntityRepository<MembershipApplication> membershipApplicationRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Student> studentRepository,
    IIdentityGateway identityGateway,
    IEmailSender emailSender)
{
    public async Task SendAsync(int membershipApplicationId)
    {
        var application = await membershipApplicationRepository
            .GetAsync(a => a.Id == membershipApplicationId)
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

        var club = await clubRepository.GetAsync(c => c.Id == application.ClubId).ConfigureAwait(false);
        var clubName = club?.Name ?? "topluluk";
        var decision = application.Status == ApplicationStatus.Approved ? "onaylandı" : "reddedildi";

        var subject = $"{clubName} üyelik başvurunuz {decision}";
        var body = $"Merhaba,\n\n{clubName} topluluğuna yaptığınız üyelik başvurusu {decision}.\n\nİyi çalışmalar.";

        await emailSender.SendAsync(studentUser.Email, subject, body).ConfigureAwait(false);
    }
}
