using Entities.Enums;

namespace Business.DTOs.Events;

public sealed class DecideEventRequestDto
{
    public EventStatus Status { get; set; }
}
