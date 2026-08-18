namespace Platform.Contracts;

public interface IEventBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}