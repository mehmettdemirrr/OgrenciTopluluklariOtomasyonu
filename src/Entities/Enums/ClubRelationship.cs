namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-41/A-70: kullanıcının BİR kulüple ilişkisi.
/// Faz 37 yalnızca Member ve Advisor üretir; kalan değerler Faz 41'in
/// (ClubDetailDto.MyRelationship) ihtiyacı için baştan ayrılmıştır — sonradan araya
/// değer sokmak, tel üzerinde metin giden bir enum'da eski istemcileri bozar.
/// </summary>
public enum ClubRelationship
{
    None = 0,
    Member = 1,
    Officer = 2,
    President = 3,
    Advisor = 4,
    Administrator = 5,
}
