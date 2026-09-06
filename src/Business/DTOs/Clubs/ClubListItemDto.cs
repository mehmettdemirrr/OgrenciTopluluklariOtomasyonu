namespace Business.DTOs.Clubs;

public sealed class ClubListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int? LogoFileId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-49/A-80: ada göre sıralı kategori adları. Boş liste = kategorisiz.
    /// Y-32: AutoMapper doldurmaz (Club üzerinde kaynağı yok, join gerekir) — profilde Ignore
    /// edilir, ClubManager tek toplu sorgudan doldurur.
    /// </summary>
    public IReadOnlyList<string> ClubCategoryNames { get; set; } = [];
}
