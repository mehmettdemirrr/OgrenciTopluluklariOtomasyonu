namespace Entities.Enums;

/// <summary>docs/MIMARI.md · A-25: taslak → onay bekliyor → yayında/reddedildi.</summary>
public enum EventStatus
{
    Draft = 0,
    PendingApproval = 1,
    Published = 2,
    Rejected = 3,
}
