using Business.Abstract;
using Core.Utilities.Email;
using Core.Utilities.Web;
using Microsoft.Extensions.Options;

namespace Business.BackgroundJobs;

/// <summary>
/// docs/MIMARI.md · Y-47: yalnızca kimlik (userId) alır, kendi scope'unu DI ile açar.
/// docs/PLAN-V2.md · Y-54: doğrulama token'ı burada, çalışma anında üretilir — Hangfire iş
/// parametresine asla yazılmaz (panelde okunabilir olması hesap devralma riski taşırdı).
/// Zaten doğrulanmış bir hesapta sessizce çıkar — idempotent, tekrar deneme güvenlidir.
/// </summary>
public sealed class EmailConfirmationJob(
    IAccountGateway accountGateway,
    IEmailSender emailSender,
    IOptions<FrontendSettings> frontendSettings)
{
    public async Task SendAsync(int userId)
    {
        var user = await accountGateway.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var token = await accountGateway.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
        var link = $"{frontendSettings.Value.BaseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        const string subject = "E-posta adresinizi doğrulayın";
        var body = $"Merhaba,\n\nÖğrenci Toplulukları Otomasyonu hesabınızı etkinleştirmek için aşağıdaki bağlantıya tıklayın:\n\n{link}\n\nBu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.";

        await emailSender.SendAsync(user.Email, subject, body).ConfigureAwait(false);
    }
}
