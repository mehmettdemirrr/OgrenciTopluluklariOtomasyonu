namespace Core.DataAccess;

/// <summary>
/// docs/PLAN-V2.md · Faz 13 (A-12): kullanımdaki bir referans veri kaydı silinmeye çalışıldığında
/// SQL Server'ın FK `Restrict` ihlali fırlattığı `DbUpdateException`'ın katman-nötr karşılığı.
/// Y-08 gereği EF Core'un DbUpdateException'ı Business'a sızamaz; somut IUnitOfWork implementasyonu
/// (AppDbContext) SaveChangesAsync içinde (concurrency dışı) bunu yakalayıp bu tipe çevirir.
/// </summary>
public sealed class ReferentialIntegrityConflictException : Exception
{
    public ReferentialIntegrityConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
