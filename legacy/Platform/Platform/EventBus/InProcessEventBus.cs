using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Contracts;

namespace Platform.EventBus;

internal sealed class InProcessEventBus(
    IServiceProvider serviceProvider,
    ILogger<InProcessEventBus> logger) : IEventBus
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var eventType = integrationEvent.GetType();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null)
                continue;

            try
            {
                var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;
                var task = (Task)method.Invoke(handler, [integrationEvent, cancellationToken])!;
                await task;
            }
            catch (Exception ex)
            {
                // Faz 0: hata loglanıp yutulur. Retry/dead-letter politikası sonraki fazda ele alınacak.
                logger.LogError(ex, "Handler {Handler}, {EventType} tipindeki olayı işlerken hata verdi.",
                    handler.GetType().Name, eventType.Name);
            }
        }
    }
}