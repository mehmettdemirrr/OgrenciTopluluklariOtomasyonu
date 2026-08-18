using Core.Entities;

namespace Entities;

/// <summary>docs/MIMARI.md · A-13: üyelik ve roller döneme bağlı.</summary>
public sealed class AcademicTerm : IEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public bool IsCurrent { get; set; }
}
