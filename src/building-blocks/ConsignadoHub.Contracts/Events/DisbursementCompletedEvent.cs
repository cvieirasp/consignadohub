using ConsignadoHub.BuildingBlocks.Messaging;

namespace ConsignadoHub.Contracts.Events;

public sealed record DisbursementCompletedEvent(
    Guid ProposalId,
    Guid DisbursementId,
    DateTimeOffset CompletedAt) : IntegrationEvent;
