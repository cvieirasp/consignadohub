using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using ProposalService.Contracts.Events;
using NotificationService.Application.Handlers;

namespace NotificationService.Worker.Consumers;

/// <summary>
/// Consumer responsible for processing ProposalSubmittedEvent messages and sending notifications accordingly. 
/// Implements idempotency using the inbox pattern to ensure that each event is processed only once, 
/// even in the case of retries or duplicates.
/// </summary>
/// <param name="publisher">The RabbitMQ event publisher used to publish events.</param>
/// <param name="settings">The RabbitMQ settings for configuring the consumer.</param>
/// <param name="scopeFactory">The service scope factory for creating scoped services.</param>
/// <param name="logger">The logger for logging information and errors.</param>
public sealed class ProposalSubmittedNotificationConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<ProposalSubmittedNotificationConsumer> logger)
    : RabbitMqConsumerBase<ProposalSubmittedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "notification.proposal.submitted";
    protected override string RoutingKey => "proposal.submitted";
    protected override string ConsumerName => "NotificationProposalSubmittedConsumer";

    protected override async Task HandleAsync(ProposalSubmittedEvent @event, CancellationToken ct)
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

        await handler.SendProposalSubmittedAsync(@event.ProposalId, @event.CorrelationId, ct);

        var inboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);
        await inboxRepository.SaveChangesAsync(ct);
    }
}
