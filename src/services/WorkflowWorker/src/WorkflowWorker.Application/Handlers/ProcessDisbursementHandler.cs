namespace WorkflowWorker.Application.Handlers;

public sealed class ProcessDisbursementHandler
{
    public (Guid DisbursementId, DateTimeOffset CompletedAt) Process(Guid proposalId)
        => (Guid.NewGuid(), DateTimeOffset.UtcNow);
}
