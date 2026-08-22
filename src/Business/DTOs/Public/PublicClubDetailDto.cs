namespace Business.DTOs.Public;

/// <summary>docs/PLAN-V2.md · Faz 14 (Y-58): AdvisorId/CreatedAtUtc gibi iç alanlar kasıtlı olarak yok.</summary>
public sealed class PublicClubDetailDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? LogoFileId { get; set; }
}
