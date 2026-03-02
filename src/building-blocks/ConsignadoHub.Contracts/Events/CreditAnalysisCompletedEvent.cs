using ConsignadoHub.BuildingBlocks.Messaging;

namespace ConsignadoHub.Contracts.Events;

public sealed record CreditAnalysisCompletedEvent(
    Guid ProposalId,
    bool Approved,
    int Score,
    string Reason) : IntegrationEvent;
