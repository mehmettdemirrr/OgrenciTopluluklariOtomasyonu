namespace Core.DataAccess;

/// <summary>
/// docs/MIMARI.md · Y-13: şemanın tek kaynağı EF migration'ları. WebAPI, DataAccess'e
/// (dolayısıyla EF Core'a) doğrudan bağımlı olamayacağı için (Y-05/Y-08) uygulama migration'larını
/// bu soyutlama üzerinden tetikler; somut implementasyon DataAccess'te yaşar.
/// </summary>
public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
