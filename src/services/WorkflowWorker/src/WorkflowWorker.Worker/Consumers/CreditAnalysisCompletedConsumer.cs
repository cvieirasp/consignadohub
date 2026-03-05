using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using WorkflowWorker.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.Worker.Consumers;

public sealed class CreditAnalysisCompletedConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    IEventPublisher eventPublisher,
    ILogger<CreditAnalysisCompletedConsumer> logger)
    : RabbitMqConsumerBase<CreditAnalysisCompletedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "workflow.credit.completed";
    protected override string RoutingKey => "proposal.credit.completed";
    protected override string ConsumerName => "WorkflowCreditAnalysisCompletedConsumer";

    protected override async Task HandleAsync(CreditAnalysisCompletedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<ProcessContractGenerationHandler>();

        // Idempotency check
        if (await inboxRepository.ExistsAsync(@event.EventId, ConsumerName, ct))
        {
            logger.LogInformation(
                "Event {EventId} already processed by {Consumer}. Skipping.",
                @event.EventId, ConsumerName);
            return;
        }

        if (!@event.Approved)
        {
            logger.LogInformation(
                "Proposal {ProposalId} was rejected. Skipping contract generation.",
                @event.ProposalId);

            var rejectedInboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
            await inboxRepository.AddAsync(rejectedInboxEntry, ct);
            await inboxRepository.SaveChangesAsync(ct);
            return;
        }

        var (contractId, contractUrl) = handler.Generate(@event.ProposalId);

        logger.LogInformation(
            "Contract generated for proposal {ProposalId}: ContractId={ContractId}.",
            @event.ProposalId, contractId);

        var contractEvent = new ContractGeneratedEvent(
            @event.ProposalId,
            contractId,
            contractUrl)
        {
            CorrelationId = @event.CorrelationId,
        };

        await eventPublisher.PublishAsync(contractEvent, "contract.generated", ct);

        var inboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);
        await inboxRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Published ContractGenerated for proposal {ProposalId}.",
            @event.ProposalId);
    }
}
