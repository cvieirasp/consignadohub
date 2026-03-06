using System.Diagnostics.CodeAnalysis;
using ConsignadoHub.BuildingBlocks.Messaging;

namespace WorkflowWorker.Contracts.Events;

[ExcludeFromCodeCoverage]
public sealed record DisbursementCompletedEvent(
    Guid ProposalId,
    Guid DisbursementId,
    DateTimeOffset CompletedAt) : IntegrationEvent;
