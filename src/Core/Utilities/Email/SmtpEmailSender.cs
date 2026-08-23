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

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            // 587 (submission) portunda STARTTLS zorunludur; bu satır olmadan Gmail/Office365
            // "5.7.0 Must issue a STARTTLS command first" ile reddeder (docs/PLAN-V3.md §15.1).
            EnableSsl = smtp.EnableSsl,
        };

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            // DefaultCredentials önce kapatılmalı — açıkken Credentials ataması yok sayılabilir.
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }

        // Y-20: gönderen adresi committen bir dosyada durmasın diye FromAddress boşsa user-secrets'taki
        // Username'e düşülür — Gmail zaten From'un kimlik doğrulanan hesapla eşleşmesini istiyor.
        var fromAddress = string.IsNullOrWhiteSpace(smtp.FromAddress) ? smtp.Username : smtp.FromAddress;
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException(
                "Smtp:FromAddress veya Smtp:Username tanımlı olmalı — gönderen adresi olmadan e-posta gönderilemez.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(toAddress);

        await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
