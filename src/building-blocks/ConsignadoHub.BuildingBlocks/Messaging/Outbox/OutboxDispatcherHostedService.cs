using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsignadoHub.BuildingBlocks.Messaging.Outbox;

public sealed class OutboxDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherHostedService> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in outbox dispatcher loop.");
            }

            await Task.Delay(Delay, stoppingToken);
        }

        logger.LogInformation("Outbox dispatcher stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await repository.FetchPendingAsync(BatchSize, ct);

        if (messages.Count == 0)
            return;

        logger.LogDebug("Processing {Count} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishRawAsync(message.Payload, message.RoutingKey, ct);
                message.MarkProcessed();

                logger.LogInformation(
                    "Published outbox message {MessageId} with routing key {RoutingKey}.",
                    message.Id, message.RoutingKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
                message.RecordFailure(ex.Message);
            }
        }

        await repository.SaveChangesAsync(ct);
    }
}
