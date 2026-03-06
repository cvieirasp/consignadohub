using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using WorkflowWorker.Contracts.Events;
using NotificationService.Application.Handlers;

namespace NotificationService.Worker.Consumers;

/// <summary>
/// Consumer responsible for handling disbursement completed events and sending notifications accordingly.
/// Implements idempotency using an inbox repository to ensure each event is processed only once.
/// </summary>
/// <param name="publisher">The RabbitMQ event publisher used to publish events.</param>
/// <param name="settings">The RabbitMQ settings for configuring the consumer.</param>
/// <param name="scopeFactory">The service scope factory for creating scoped services.</param>
/// <param name="logger">The logger for logging information and errors.</param>
public sealed class DisbursementCompletedNotificationConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<DisbursementCompletedNotificationConsumer> logger)
    : RabbitMqConsumerBase<DisbursementCompletedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "notification.disbursement.completed";
    protected override string RoutingKey => "disbursement.completed";
    protected override string ConsumerName => "NotificationDisbursementCompletedConsumer";

    protected override async Task HandleAsync(DisbursementCompletedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<NotificationHandler>();

        // Idempotency check
        if (await inboxRepository.ExistsAsync(@event.EventId, ConsumerName, ct))
        {
            logger.LogInformation(
                "Event {EventId} already processed by {Consumer}. Skipping.",
                @event.EventId, ConsumerName);
            return;
        }

        await handler.SendDisbursementCompletedAsync(@event.ProposalId, @event.CompletedAt, @event.CorrelationId, ct);

        var inboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);
        await inboxRepository.SaveChangesAsync(ct);
    }
}
