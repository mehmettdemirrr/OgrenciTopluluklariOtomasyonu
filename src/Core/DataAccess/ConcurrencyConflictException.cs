namespace Core.DataAccess;

/// <summary>
/// docs/PLAN-V2.md · A-38/Y-53: RowVersion eşzamanlılık ihlalinin katman-nötr karşılığı.
/// Y-08 gereği EF Core'un DbUpdateConcurrencyException'ı Business'a sızamaz; somut
/// IUnitOfWork implementasyonu (AppDbContext) SaveChangesAsync içinde bunu yakalayıp bu tipe çevirir.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
