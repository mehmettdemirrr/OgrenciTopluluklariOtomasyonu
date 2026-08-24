using Entities.Enums;

namespace Business.DTOs.Clubs;

/// <summary>docs/PLAN-V2.md · Faz 12: GET /api/clubs/mine — çağıranın kendi üyelikleri.</summary>
public sealed class MyClubMembershipDto
{
    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public bool ClubIsActive { get; set; }

    public ClubRole ClubRole { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    /// <summary>docs/PLAN-V4.md §22.3: liste güncel döneme filtreli — hangi dönem olduğu görünsün.</summary>
    public required string AcademicTermName { get; set; }
}
