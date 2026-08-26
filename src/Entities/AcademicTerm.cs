using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>docs/MIMARI.md · A-13: üyelik ve roller döneme bağlı. K-39/A-66: başvuru penceresini de taşır.</summary>
public sealed class AcademicTerm : IEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public bool IsCurrent { get; set; }

    /// <summary>docs/MIMARI.md · K-39/A-66: topluluk kurma başvurularının açıldığı an. Null = takvim tanımsız.</summary>
    public DateTime? ClubApplicationStartUtc { get; set; }

    /// <summary>docs/MIMARI.md · K-39/A-66: başvuruların kapandığı an. Null = takvim tanımsız.</summary>
    public DateTime? ClubApplicationEndUtc { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-39/A-66: takvimi geçersiz kılma. Varsayılan FollowSchedule —
    /// tarihler tanımsızken bu KAPALI demektir (fail-closed); başvuru sezonuna kurum karar verir.
    /// </summary>
    public ClubApplicationWindowOverride ClubApplicationOverride { get; set; }
}
