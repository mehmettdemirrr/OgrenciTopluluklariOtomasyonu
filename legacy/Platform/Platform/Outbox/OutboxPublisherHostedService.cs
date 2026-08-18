using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Contracts;

namespace Platform.Outbox;

public sealed class OutboxPublisherHostedService(
    IEnumerable<IOutboxStore> outboxStores,
    IEventBus eventBus,
    IEventTypeRegistry typeRegistry,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);
        do
        {
            await ProcessOnceAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Tek bir polling turunu senkronize biçimde çalıştırır. Testlerin, background döngüyü
    /// beklemek yerine outbox'ı deterministik olarak tetiklemesini sağlar.
    /// </summary>
    public async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var store in outboxStores)
        {
            IReadOnlyList<OutboxMessageRecord> pending;
            try
            {
                pending = await store.GetUnpublishedAsync(BatchSize, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Module} outbox'ı okunamadı.", store.ModuleName);
                continue;
            }

            foreach (var message in pending)
            {
                try
                {
                    var clrType = typeRegistry.Resolve(message.Type);
                    var integrationEvent = (IIntegrationEvent?)JsonSerializer.Deserialize(message.Payload, clrType)
                        ?? throw new InvalidOperationException($"Outbox mesajı deserialize edilemedi: {message.Id}");

                    await eventBus.PublishAsync(integrationEvent, cancellationToken);
                    await store.MarkPublishedAsync(message.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Module} outbox mesajı {MessageId} yayınlanamadı.", store.ModuleName, message.Id);
                    await store.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
                }
            }
        }
    }
}