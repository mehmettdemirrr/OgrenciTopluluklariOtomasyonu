using System.Collections.Concurrent;
using Core.Utilities.Email;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md: testlerde gerçek SMTP sunucusuna asla bağlanılmaz — CustomWebApplicationFactory
/// bunu tek instance (SingleInstance) olarak IEmailSender yerine kaydeder.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    public ConcurrentBag<(string ToAddress, string Subject, string Body)> SentEmails { get; } = [];

    public Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((toAddress, subject, body));
        return Task.CompletedTask;
    }
}
