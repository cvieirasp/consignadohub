using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using WorkflowWorker.Contracts.Events;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.Worker.Consumers;

/// <summary>
/// Consumer responsible for handling ContractGeneratedEvent, which is published after 
/// a contract is generated for an approved proposal. It processes the event to trigger 
/// disbursement and ensures idempotency using the inbox pattern.
/// </summary>
/// <param name="publisher">The RabbitMQ event publisher used to publish events.</param>
/// <param name="settings">The RabbitMQ settings for configuring the consumer.</param>
/// <param name="scopeFactory">The service scope factory for creating scoped services.</param>
/// <param name="eventPublisher">The event publisher for publishing domain events.</param>
/// <param name="logger">The logger for logging consumer activities.</param>
public sealed class ContractGeneratedConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    IEventPublisher eventPublisher,
    ILogger<ContractGeneratedConsumer> logger)
    : RabbitMqConsumerBase<ContractGeneratedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "workflow.contract.generated";
    protected override string RoutingKey => "contract.generated";
    protected override string ConsumerName => "WorkflowContractGeneratedConsumer";

    protected override async Task HandleAsync(ContractGeneratedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<ProcessDisbursementHandler>();

        // Idempotency check
        if (await inboxRepository.ExistsAsync(@event.EventId, ConsumerName, ct))
        {
            logger.LogInformation(
                "Event {EventId} already processed by {Consumer}. Skipping.",
                @event.EventId, ConsumerName);
            return;
        }

        var (disbursementId, completedAt) = handler.Process(@event.ProposalId);

        logger.LogInformation(
            "Disbursement processed for proposal {ProposalId}: DisbursementId={DisbursementId}.",
            @event.ProposalId, disbursementId);

        var disbursementEvent = new DisbursementCompletedEvent(
            @event.ProposalId,
            disbursementId,
            completedAt)
        {
            CorrelationId = @event.CorrelationId,
        };

        await eventPublisher.PublishAsync(disbursementEvent, "disbursement.completed", ct);

        var inboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);
        await inboxRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Published DisbursementCompleted for proposal {ProposalId}.",
            @event.ProposalId);
    }
}
