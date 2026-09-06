namespace Business.DTOs.Clubs;

public sealed class UpdateClubRequestDto
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-33 (PLAN-V5 §27.2): kulübün danışmanı. v5.0'a kadar bu alan yoktu —
    /// danışman yalnızca kulüp oluşturulurken belirlenip bir daha değiştirilemiyordu
    /// (PLAN-V5 bildirilen #3).
    ///
    /// <b>Yetki devri:</b> değişim, eski danışmanın o kulüpteki tüm yetkisini (etkinlik
    /// oluşturma/onaylama, üye rolü, logo) anında kaldırır — `Ensure*` metotları
    /// <c>club.AdvisorId</c>'yi okuduğu için ek bir iş gerekmez.
    ///
    /// <c>null</c> gönderilirse mevcut danışman korunur (kısmi güncelleme).
    /// </summary>
    public int? AdvisorId { get; set; }

    /// <summary>docs/MIMARI.md · K-49: en fazla 3 (validator). Boş liste = kategorisiz.</summary>
    public IReadOnlyList<int> ClubCategoryIds { get; set; } = [];
}
