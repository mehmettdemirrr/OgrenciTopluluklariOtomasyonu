namespace Core.Utilities.Security;

/// <summary>
/// docs/MIMARI.md · A-33/A-29: arka plan işlerinin (Hangfire) HTTP bağlamı yoktur — audit ve iş
/// kuralları bu sistem kullanıcısını görür. İş sınıfları yetki kararını buna değil kendi iş mantığına
/// (örn. ReportRequest.RequestedByUserId) dayandırır.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public int? UserId => null;

    public bool IsAuthenticated => false;

    public IReadOnlyCollection<string> Permissions { get; } = [];
}
