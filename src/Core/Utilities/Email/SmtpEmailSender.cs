using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Serilog;

namespace Core.Utilities.Email;

/// <summary>
/// docs/MIMARI.md · K-03: BCL SmtpClient — ek paket gerekmez. Smtp:Host boşsa gerçek gönderim
/// atlanır, Serilog'a loglanır — SMTP sunucusuz geliştirme/demo ortamında akışın kesilmemesi için
/// (ConnectionStrings:LogDb boşken yalnızca konsola yazan A-18 deseniyle aynı fikir).
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpSettings> settings) : IEmailSender
{
    public async Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtp = settings.Value;

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            // Gövde de loglanır (yalnızca yerel Serilog konsol çıktısı, kalıcı bir yere yazılmaz) —
            // SMTP sunucusuz geliştirmede doğrulama/sıfırlama bağlantısı buradan okunup denenebilsin diye.
            Log.Information(
                "Smtp:Host boş — e-posta gönderilmedi, yalnızca loglandı. Alıcı: {ToAddress}, Konu: {Subject}\n{Body}",
                toAddress, subject, body);
            return;
        }

        using var client = new SmtpClient(smtp.Host, smtp.Port);
        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress, smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(toAddress);

        await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
