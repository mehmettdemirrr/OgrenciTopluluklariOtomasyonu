namespace Core.Utilities.Email;

/// <summary>docs/MIMARI.md · K-03: SMTP tabanlı e-posta gönderimi.</summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
