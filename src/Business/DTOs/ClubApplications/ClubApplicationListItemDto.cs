using Entities.Enums;

namespace Business.DTOs.ClubApplications;

public sealed class ClubApplicationListItemDto
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public required string StudentNumber { get; set; }

    public required string ProposedName { get; set; }

    public string? Description { get; set; }

    public required string Justification { get; set; }

    public int ProposedAdvisorId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-56: unvan + ad soyad ("Prof. Dr. Elif Yıldırım"). Faz 26'ya kadar
    /// sistemde ad soyad yoktu ve bu alan yalnızca unvanı taşıyordu — inceleme ekranında
    /// "Prof. Dr." yazıp kimi kastettiğini söylemiyordu.
    /// </summary>
    public required string ProposedAdvisorDisplayName { get; set; }

    public int? ProposedCategoryId { get; set; }

    /// <summary>İnceleme ekranı adı gösterir; kategori seçilmemişse null.</summary>
    public string? ProposedCategoryName { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewNote { get; set; }

    public int? CreatedClubId { get; set; }
}
