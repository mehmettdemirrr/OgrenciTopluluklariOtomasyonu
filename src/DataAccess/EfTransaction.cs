using Core.DataAccess;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess;

/// <summary>
/// docs/MIMARI.md · Y-08: IDbContextTransaction hiçbir zaman DataAccess dışına sızmaz —
/// somut tip internal, arayüz (ITransaction) public.
/// </summary>
internal sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
