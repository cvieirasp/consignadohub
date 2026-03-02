using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using ConsignadoHub.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Handlers;

namespace NotificationService.Worker.Consumers;

public sealed class ContractGeneratedNotificationConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<ContractGeneratedNotificationConsumer> logger)
    : RabbitMqConsumerBase<ContractGeneratedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "notification.contract.generated";
    protected override string RoutingKey => "contract.generated";
    protected override string ConsumerName => "NotificationContractGeneratedConsumer";

    protected override async Task HandleAsync(ContractGeneratedEvent @event, CancellationToken ct)
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

        await handler.SendContractGeneratedAsync(@event.ProposalId, @event.ContractUrl, @event.CorrelationId, ct);

        var inboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);
        await inboxRepository.SaveChangesAsync(ct);
    }
}
