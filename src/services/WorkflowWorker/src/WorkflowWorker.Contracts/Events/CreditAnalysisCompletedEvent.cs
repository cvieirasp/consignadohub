using ConsignadoHub.BuildingBlocks.Messaging;

namespace WorkflowWorker.Contracts.Events;

public sealed record CreditAnalysisCompletedEvent(
    Guid ProposalId,
    bool Approved,
    int Score,
    string Reason) : IntegrationEvent;
