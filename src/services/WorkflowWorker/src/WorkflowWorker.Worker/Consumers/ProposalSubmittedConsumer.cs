using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using ConsignadoHub.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.Worker.Consumers;

public sealed class ProposalSubmittedConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    IEventPublisher eventPublisher,
    ILogger<ProposalSubmittedConsumer> logger)
    : RabbitMqConsumerBase<ProposalSubmittedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "proposal.submitted";
    protected override string RoutingKey => "proposal.submitted";
    protected override string ConsumerName => "ProposalSubmittedConsumer";

    protected override async Task HandleAsync(ProposalSubmittedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<ProcessCreditAnalysisHandler>();

        // Idempotency check
        if (await inboxRepository.ExistsAsync(@event.EventId, ConsumerName, ct))
        {
            logger.LogInformation(
                "Event {EventId} already processed by {Consumer}. Skipping.",
                @event.EventId, ConsumerName);
            return;
        }

        var (approved, score, reason) = handler.Analyze(
            @event.ProposalId,
            @event.RequestedAmount,
            @event.TermMonths);

        logger.LogInformation(
            "Credit analysis for proposal {ProposalId}: Score={Score}, Approved={Approved}.",
            @event.ProposalId, score, approved);

        var completedEvent = new CreditAnalysisCompletedEvent(
            @event.ProposalId,
            approved,
            score,
            reason)
        {
            CorrelationId = @event.CorrelationId,
        };

        await eventPublisher.PublishAsync(completedEvent, "proposal.credit.completed", ct);

        var inboxEntry = InboxMessage.Create(@event.EventId, ConsumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);
        await inboxRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Published CreditAnalysisCompleted for proposal {ProposalId}.",
            @event.ProposalId);
    }
}
