namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-39/A-66/Y-73: başvuru penceresinin takvimi geçersiz kılma durumu.
/// DB'de int, API'de metin (sessiz onay).
/// </summary>
public enum ClubApplicationWindowOverride
{
    /// <summary>Takvime uy: tarihler tanımlıysa aralıkta açık, tanımsızsa KAPALI (fail-closed).</summary>
    FollowSchedule = 0,

    /// <summary>Tarihlere bakılmaksızın açık.</summary>
    ForceOpen = 1,

    /// <summary>Tarihlere bakılmaksızın kapalı.</summary>
    ForceClosed = 2,
}
