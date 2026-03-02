using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Outbox;
using ConsignadoHub.BuildingBlocks.Results;
using ConsignadoHub.Contracts.Events;
using FluentValidation;
using Microsoft.Extensions.Options;
using ProposalService.Application.Configuration;
using ProposalService.Application.DTOs;
using ProposalService.Application.Ports;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Errors;

namespace ProposalService.Application.UseCases;

public sealed class SubmitProposalUseCase(
    IProposalRepository repository,
    IOutboxRepository outboxRepository,
    IValidator<SubmitProposalInput> validator,
    IOptions<ProposalSettings> settings)
{
    private readonly ProposalSettings _settings = settings.Value;

    public async Task<Result<Guid>> ExecuteAsync(SubmitProposalInput input, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ProposalErrors.ValidationFailed(string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));

        if (input.RequestedAmount < 100m || input.RequestedAmount > 500_000m)
            return ProposalErrors.InvalidAmount;

        if (input.TermMonths < 6 || input.TermMonths > 120)
            return ProposalErrors.InvalidTerm;

        var rate = input.MonthlyRate ?? _settings.DefaultMonthlyRate;
        var proposal = Proposal.Create(input.CustomerId, input.RequestedAmount, input.TermMonths, rate);

        proposal.AddTimelineEntry(null, proposal.Status);

        var @event = new ProposalSubmittedEvent(
            proposal.Id,
            proposal.CustomerId,
            proposal.RequestedAmount,
            proposal.TermMonths);

        var outboxMessage = OutboxMessage.Create(@event, "proposal.submitted");

        await repository.AddAsync(proposal, ct);
        await outboxRepository.AddAsync(outboxMessage, ct);

        // Proposal + outbox message saved atomically in the same DbContext transaction
        await repository.SaveChangesAsync(ct);

        return proposal.Id;
    }
}
