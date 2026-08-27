using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · A-13/A-15: dönemsel üyelik, topluluk rolü.
/// Y-16: soft delete + query filter. Y-18: unique index (ClubId, StudentId, AcademicTermId).
/// </summary>
public sealed class ClubMembership : IEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public int StudentId { get; set; }

    public int AcademicTermId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-68: <b>makam.</b> A-39 başkan tekilliği, dönem devri ve bildirim hedefi
    /// bunu kullanır. <b>Yetki kararı vermez</b> — o <see cref="Capabilities"/>'ten okunur.
    /// </summary>
    public ClubRole ClubRole { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-36/A-61: görünen unvan. Null = unvansız.
    /// Atama ve tanım değişikliği unvanı, makamı ve kapasiteyi birlikte yazar (O-27).
    /// </summary>
    public int? ClubRoleDefinitionId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-61/A-68: yetki matrisinin denormalize kopyası. <b>Yetki kararı BURADAN okunur</b> —
    /// bu sayede kontroller tek satır okur, tanım tablosuna ikinci bir sorgu gitmez.
    /// Unvansız üyede <c>ClubCapabilityDefaults.ForRole(ClubRole)</c> ile doldurulur.
    /// </summary>
    public ClubCapability Capabilities { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
