using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Results;
using ConsignadoHub.Contracts.Events;
using Microsoft.Extensions.Logging;
using ProposalService.Application.Ports;
using ProposalService.Application.Exceptions;
using ProposalService.Domain.Enums;
using ProposalService.Domain.Errors;

namespace ProposalService.Application.UseCases;

public sealed class HandleCreditAnalysisCompletedUseCase(
    IProposalRepository proposalRepository,
    IInboxRepository inboxRepository)
{
    public async Task<Result> ExecuteAsync(CreditAnalysisCompletedEvent @event, CancellationToken ct = default)
    {
        const string consumerName = "CreditAnalysisCompletedConsumer";

        // Idempotency check
        if (await inboxRepository.ExistsAsync(@event.EventId, consumerName, ct))
            return Result.Success();

        var proposal = await proposalRepository.GetByIdForUpdateAsync(@event.ProposalId, ct);

        if (proposal is null)
            return ProposalErrors.NotFound(@event.ProposalId);

        var newStatus = @event.Approved ? ProposalStatus.Approved : ProposalStatus.Rejected;
        var result = proposal.UpdateStatus(newStatus, @event.Reason);

        if (result.IsFailure)
            return result;

        var inboxEntry = InboxMessage.Create(@event.EventId, consumerName);
        await inboxRepository.AddAsync(inboxEntry, ct);

        // Proposal update + inbox entry saved atomically in the same DbContext transaction
        await proposalRepository.SaveChangesAsync(ct);

        return Result.Success();
    }
}
