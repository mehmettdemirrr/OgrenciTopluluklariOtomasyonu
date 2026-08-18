using System.Collections.Concurrent;
using Platform.Contracts;

namespace Platform.EventBus;

internal sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _typesByName = new();

    public void Register<TEvent>(string typeName) where TEvent : IIntegrationEvent
    {
        _typesByName[typeName] = typeof(TEvent);
    }

    public Type Resolve(string typeName)
    {
        if (_typesByName.TryGetValue(typeName, out var type))
            return type;

        throw new InvalidOperationException($"Olay tipi kayıtlı değil: '{typeName}'.");
    }
}