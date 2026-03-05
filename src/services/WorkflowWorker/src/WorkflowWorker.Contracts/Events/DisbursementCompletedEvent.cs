using ConsignadoHub.BuildingBlocks.Messaging;

namespace WorkflowWorker.Contracts.Events;

public sealed record DisbursementCompletedEvent(
    Guid ProposalId,
    Guid DisbursementId,
    DateTimeOffset CompletedAt) : IntegrationEvent;
