namespace Core.DataAccess;

/// <summary>
/// docs/MIMARI.md · A-06/Y-08: EF'in IDbContextTransaction'ı Core'a sızmaz — TransactionAspect
/// yalnızca bu soyutlamayı bilir, somut implementasyon DataAccess'te (EfTransaction) yaşar.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
