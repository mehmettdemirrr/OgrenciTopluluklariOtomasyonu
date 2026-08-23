using Entities.Enums;

namespace Business.DTOs.ClubApplications;

public sealed class DecideClubApplicationRequestDto
{
    public ApplicationStatus Status { get; set; }

    public string? ReviewNote { get; set; }
}
