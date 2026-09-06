namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-82/A-77: sayaç tek SQL UPDATE ile artar (Club.RowVersion çakışmasın).</summary>
public interface IClubViewDal
{
    /// <summary>Eşleşen satır yoksa 0 döner (kulüp yok ya da pasif).</summary>
    Task<int> IncrementAsync(int clubId, CancellationToken cancellationToken = default);
}
