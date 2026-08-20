using Entities.Enums;

namespace Business.DTOs.Memberships;

/// <summary>
/// docs/MIMARI.md · Y-32: Club/Student ile çapraz veri taşıdığı için AutoMapper'dan değil,
/// MembershipApplicationManager içinde elle derlenir.
/// </summary>
public sealed class MembershipApplicationListItemDto
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public int StudentId { get; set; }

    public required string StudentNumber { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAtUtc { get; set; }
}
