using Entities.Enums;

namespace Business.DTOs.Memberships;

public sealed class ReviewMembershipApplicationRequestDto
{
    public ApplicationStatus Status { get; set; }
}
