using Business.Abstract;
using Core.Utilities.Email;
using Core.Utilities.Web;
using Microsoft.Extensions.Options;

namespace Business.BackgroundJobs;

/// <summary>
/// docs/MIMARI.md · Y-47: yalnızca userId alır. docs/PLAN-V2.md · Y-54: sıfırlama token'ı burada,
/// çalışma anında üretilir — asla Hangfire iş parametresine yazılmaz.
/// </summary>
public sealed class PasswordResetEmailJob(
    IAccountGateway accountGateway,
    IEmailSender emailSender,
    IOptions<FrontendSettings> frontendSettings)
{
    public async Task SendAsync(int userId)
    {
        var user = await accountGateway.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var token = await accountGateway.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var link = $"{frontendSettings.Value.BaseUrl}/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        const string subject = "Parola sıfırlama isteği";
        var body = $"Merhaba,\n\nParolanızı sıfırlamak için aşağıdaki bağlantıya tıklayın:\n\n{link}\n\nBu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.";

        await emailSender.SendAsync(user.Email, subject, body).ConfigureAwait(false);
    }
}
