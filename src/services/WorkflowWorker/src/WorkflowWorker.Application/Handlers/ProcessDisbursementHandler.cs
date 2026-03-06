namespace WorkflowWorker.Application.Handlers;

/// <summary>
/// Simulates the disbursement process for a loan application. 
/// In a real-world scenario, this would involve complex logic to interact with 
/// financial systems, update records, and ensure compliance with regulations. 
/// Here, we simply generate a new disbursement ID and return the current timestamp 
/// to indicate when the disbursement was completed.
/// </summary>
public sealed class ProcessDisbursementHandler
{
    public (Guid DisbursementId, DateTimeOffset CompletedAt) Process(Guid proposalId)
        => (Guid.NewGuid(), DateTimeOffset.UtcNow);
}
