namespace Business.DTOs.Clubs;

public sealed class ClubDetailDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int AdvisorId { get; set; }

    public int? LogoFileId { get; set; }

    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }

    /// <summary>
    /// Kategori adı. Y-32: AutoMapper doldurmaz (Club üzerinde kaynağı yok, join gerekir) —
    /// profilde Ignore edilir, ClubManager kategori sözlüğünden doldurur.
    /// </summary>
    public string? ClubCategoryName { get; set; }
}
