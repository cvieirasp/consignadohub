using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using ConsignadoHub.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Handlers;

namespace NotificationService.Worker.Consumers;

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
