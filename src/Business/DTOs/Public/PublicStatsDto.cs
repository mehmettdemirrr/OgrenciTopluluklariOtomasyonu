namespace Business.DTOs.Public;

/// <summary>
/// Anonim vitrin özeti — yalnızca toplu sayılar. Kişisel veri (e-posta, ad, öğrenci no) taşınmaz (Y-58).
/// </summary>
public sealed class PublicStatsDto
{
    public int ClubCount { get; init; }

    public int ActiveClubCount { get; init; }

    public int StudentCount { get; init; }

    public int UpcomingEventCount { get; init; }
}
