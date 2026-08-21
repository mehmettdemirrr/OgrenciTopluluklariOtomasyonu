using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class ClubMemberListItemDto
{
    public int MembershipId { get; set; }

    public int StudentId { get; set; }

    public required string StudentNumber { get; set; }

    public ClubRole ClubRole { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
