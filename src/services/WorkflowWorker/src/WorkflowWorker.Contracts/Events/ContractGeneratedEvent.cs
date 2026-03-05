using ConsignadoHub.BuildingBlocks.Messaging;

namespace WorkflowWorker.Contracts.Events;

public sealed record ContractGeneratedEvent(
    Guid ProposalId,
    Guid ContractId,
    string ContractUrl) : IntegrationEvent;
