using Core.Entities;

namespace Entities;

/// <summary>docs/MIMARI.md · A-12/A-27: referans verisi, hard delete serbest.</summary>
public sealed class Faculty : IEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
