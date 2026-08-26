using Entities.Enums;

namespace Business.DTOs.Reference;

/// <summary>
/// docs/MIMARI.md · K-39/A-66: dönemin başvuru penceresi. Üç alan BİRLİKTE yazılır —
/// kısmi güncelleme yok, yönetici her seferinde tam durumu bildirir.
/// </summary>
public sealed class SetClubApplicationWindowRequestDto
{
    /// <summary>Null = takvim tanımsız. FollowSchedule ile birlikte pencereyi kapatır (fail-closed).</summary>
    public DateTime? StartUtc { get; set; }

    /// <summary>Null = takvim tanımsız.</summary>
    public DateTime? EndUtc { get; set; }

    public ClubApplicationWindowOverride Override { get; set; }
}
