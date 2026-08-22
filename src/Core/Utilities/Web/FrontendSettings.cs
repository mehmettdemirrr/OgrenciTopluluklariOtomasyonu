namespace Core.Utilities.Web;

/// <summary>
/// docs/PLAN-V2.md · Faz 11: e-posta doğrulama/şifre sıfırlama bağlantıları bu adrese kurulur.
/// Y-20: secret değil, appsettings'te durabilir. Boşsa iş (job) göreli bir bağlantı üretir —
/// üretimde arayuz aynı origin'den servis edildiğinde bu zaten doğru davranıştır.
/// </summary>
public sealed class FrontendSettings
{
    public string BaseUrl { get; init; } = string.Empty;
}
