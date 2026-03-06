using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Handlers;

/// <summary>
/// Handles outbound notification side effects for domain events consumed from RabbitMQ.
/// Each method is intentionally a stub: in production, replace the log call with a real
/// delivery mechanism (e.g. SendGrid, SMTP, webhook HTTP call).
/// NotificationService never mutates domain state — it is a pure fan-out consumer.
/// </summary>
public sealed class NotificationHandler(ILogger<NotificationHandler> logger)
{
    public Task SendProposalSubmittedAsync(Guid proposalId, string correlationId, CancellationToken ct)
    {
        logger.LogInformation(
            "[NOTIFICATION] Proposal {ProposalId} submitted. Sending confirmation email stub. CorrelationId={CorrelationId}",
            proposalId, correlationId);
        return Task.CompletedTask;
    }

    public Task SendCreditAnalysisCompletedAsync(Guid proposalId, bool approved, string correlationId, CancellationToken ct)
    {
        logger.LogInformation(
            "[NOTIFICATION] Credit analysis completed for proposal {ProposalId}. Approved={Approved}. Sending result email stub. CorrelationId={CorrelationId}",
            proposalId, approved, correlationId);
        return Task.CompletedTask;
    }

    public Task SendContractGeneratedAsync(Guid proposalId, string contractUrl, string correlationId, CancellationToken ct)
    {
        logger.LogInformation(
            "[NOTIFICATION] Contract generated for proposal {ProposalId}. ContractUrl={ContractUrl}. Sending contract email stub. CorrelationId={CorrelationId}",
            proposalId, contractUrl, correlationId);
        return Task.CompletedTask;
    }

    public Task SendDisbursementCompletedAsync(Guid proposalId, DateTimeOffset completedAt, string correlationId, CancellationToken ct)
    {
        logger.LogInformation(
            "[NOTIFICATION] Disbursement completed for proposal {ProposalId}. CompletedAt={CompletedAt:O}. Sending disbursement email stub. CorrelationId={CorrelationId}",
            proposalId, completedAt, correlationId);
        return Task.CompletedTask;
    }
}
