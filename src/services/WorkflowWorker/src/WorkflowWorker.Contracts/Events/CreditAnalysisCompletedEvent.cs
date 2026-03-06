using System.Diagnostics.CodeAnalysis;
using ConsignadoHub.BuildingBlocks.Messaging;

namespace WorkflowWorker.Contracts.Events;

[ExcludeFromCodeCoverage]
public sealed record CreditAnalysisCompletedEvent(
    Guid ProposalId,
    bool Approved,
    int Score,
    string Reason) : IntegrationEvent;
