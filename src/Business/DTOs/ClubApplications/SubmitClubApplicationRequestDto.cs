namespace Business.DTOs.ClubApplications;

public sealed class SubmitClubApplicationRequestDto
{
    public string ProposedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Justification { get; set; } = string.Empty;

    public int ProposedAdvisorId { get; set; }
}
