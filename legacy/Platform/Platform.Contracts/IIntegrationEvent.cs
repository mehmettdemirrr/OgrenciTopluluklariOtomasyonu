namespace Platform.Contracts;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}