namespace Core.DataAccess;

/// <summary>
/// docs/MIMARI.md · A-05: repository işaretler, Business burada açıkça kaydeder.
/// Somut implementasyonu DataAccess'teki AppDbContext sağlar (Faz 4).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · A-06: TransactionAspect ve elle yönetilen transaction'lar için (Y-46).</summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
