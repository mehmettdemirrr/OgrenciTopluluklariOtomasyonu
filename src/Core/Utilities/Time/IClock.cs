namespace Core.Utilities.Time;

/// <summary>
/// docs/MIMARI.md · Y-17: DateTime.Now yasak. Bu soyutlama, süre/rotasyon kararlarının
/// (ör. RefreshToken geçerliliği) testte mock'lanabilir bir "şimdi" üzerinden çalışmasını sağlar.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
