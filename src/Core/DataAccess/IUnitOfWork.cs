namespace Core.DataAccess;

/// <summary>
/// docs/MIMARI.md · A-05: repository işaretler, Business burada açıkça kaydeder.
/// Somut implementasyonu DataAccess'teki AppDbContext sağlar (Faz 4).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
