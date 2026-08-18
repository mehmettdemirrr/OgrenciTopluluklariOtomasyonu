namespace Platform.Contracts;

public interface IEventTypeRegistry
{
    void Register<TEvent>(string typeName) where TEvent : IIntegrationEvent;

    Type Resolve(string typeName);
}