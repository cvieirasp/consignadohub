using System.Diagnostics.CodeAnalysis;
using ConsignadoHub.BuildingBlocks.Messaging;

namespace WorkflowWorker.Contracts.Events;

[ExcludeFromCodeCoverage]
public sealed record ContractGeneratedEvent(
    Guid ProposalId,
    Guid ContractId,
    string ContractUrl) : IntegrationEvent;
