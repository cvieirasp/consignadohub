using ConsignadoHub.BuildingBlocks.Messaging;

namespace ConsignadoHub.Contracts.Events;

public sealed record ProposalSubmittedEvent(
    Guid ProposalId,
    Guid CustomerId,
    decimal RequestedAmount,
    int TermMonths) : IntegrationEvent;
