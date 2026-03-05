using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Results;
using WorkflowWorker.Contracts.Events;
using Microsoft.Extensions.Logging;
using ProposalService.Application.Ports;
using ProposalService.Domain.Enums;
using ProposalService.Domain.Errors;

namespace ProposalService.Application.UseCases;

public sealed class HandleDisbursementCompletedUseCase(
    IProposalRepository proposalRepository,
    IInboxRepository inboxRepository)
{
    public async Task<Result> ExecuteAsync(DisbursementCompletedEvent @event, CancellationToken ct = default)
    {
        const string consumerName = "DisbursementCompletedConsumer";

        // Idempotency check
        if (await inboxRepository.ExistsAsync(@event.EventId, consumerName, ct))
            return Result.Success();

        var proposal = await proposalRepository.GetByIdForUpdateAsync(@event.ProposalId, ct);

        if (proposal is null)
            return ProposalErrors.NotFound(@event.ProposalId);

        var reason = $"Disbursement completed. DisbursementId: {@event.DisbursementId}. CompletedAt: {@event.CompletedAt:O}.";
        var result = proposal.UpdateStatus(ProposalStatus.Disbursed, reason);

        if (result.IsFailure)
            return result;

        var inboxEntry = InboxMessage.Create(@event.EventId, consumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);

        // Proposal update + inbox entry saved atomically in the same DbContext transaction
        await proposalRepository.SaveChangesAsync(ct);

        return Result.Success();
    }
}
