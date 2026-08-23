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

    public required string ProposedAdvisorTitle { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewNote { get; set; }

    public int? CreatedClubId { get; set; }
}
