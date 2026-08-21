namespace Business.DTOs.Events;

public sealed class EventParticipantListItemDto
{
    public int StudentId { get; set; }

    public required string StudentNumber { get; set; }

    public DateTime RegisteredAtUtc { get; set; }
}
