namespace Platform.Contracts;

/// <summary>
/// Her modülün kendi şemasına kendi migration'ını uygulamasını sağlayan uç nokta.
/// Şemalar arası FK olmadığı için modüller arası uygulama sırası önemsizdir.
/// </summary>
public interface IModuleMigrator
{
    string ModuleName { get; }

    Task MigrateAsync(CancellationToken cancellationToken);
}