namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-81/Y-42: vitrin kartı sayıları — sayfa başına TEK GROUP BY sorgusu.</summary>
public interface IClubStatsDal
{
    Task<IReadOnlyDictionary<int, ClubCardStats>> GetCountsAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default);
}

/// <summary>A-81: üye = GÜNCEL dönemin üyelikleri; etkinlik = vitrinde görünen (Published + Public) etkinlikler.</summary>
public sealed record ClubCardStats(int MemberCount, int EventCount);
