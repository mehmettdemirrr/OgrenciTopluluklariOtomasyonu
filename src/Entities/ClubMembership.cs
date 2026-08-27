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

    public ClubRole ClubRole { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-36/A-61: görünen unvan. Null = unvansız (yalnızca yetki seviyesi).
    /// <b>Yetki kararı bu alandan OKUNMAZ</b> — <see cref="ClubRole"/> yetkinin tek kaynağı olarak kalır.
    /// İkisi asla ayrışmaz: atama ve seviye değişikliği ikisini birlikte yazar (O-27).
    /// </summary>
    public int? ClubRoleDefinitionId { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
