namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · A-77: görüntülenme sayacı tek SQL UPDATE ile artar. Generic repository'nin
/// oku-değiştir-kaydet döngüsü `Event.RowVersion` yüzünden eşzamanlı isteklerde çakışırdı (A-15/Y-53).
/// </summary>
public interface IEventViewDal
{
    /// <summary>Eşleşen satır yoksa 0 döner (etkinlik yok ya da vitrin filtresine uymuyor).</summary>
    Task<int> IncrementAsync(int eventId, CancellationToken cancellationToken = default);
}
