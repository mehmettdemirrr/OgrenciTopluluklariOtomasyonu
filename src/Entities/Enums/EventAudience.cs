namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-38/A-65/Y-72: etkinliğe kimin katılabileceği. AnnouncementVisibility'nin
/// (A-43) birebir kardeşi — yazma anında belirlenir, okuma anında yorumlanmaz.
/// DB'de int, API'de metin (sessiz onay).
/// </summary>
public enum EventAudience
{
    /// <summary>Herkese açık: her öğrenci kaydolabilir, anonim vitrinde görünür.</summary>
    Public = 0,

    /// <summary>Yalnızca topluluk üyelerine: güncel dönem üyeliği şart, anonim vitrinde görünmez.</summary>
    ClubMembers = 1,
}
