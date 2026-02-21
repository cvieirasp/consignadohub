using ConsignadoHub.BuildingBlocks.Results;
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
    IValidator<SubmitProposalInput> validator,
    IOptions<ProposalSettings> settings)
{
    private readonly ProposalSettings _settings = settings.Value;

    public async Task<Result<Guid>> ExecuteAsync(SubmitProposalInput input, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ProposalErrors.ValidationFailed(string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage))); ;

        if (input.RequestedAmount < 100m || input.RequestedAmount > 500_000m)
            return ProposalErrors.InvalidAmount;

        if (input.TermMonths < 6 || input.TermMonths > 120)
            return ProposalErrors.InvalidTerm;

        var rate = input.MonthlyRate ?? _settings.DefaultMonthlyRate;
        var proposal = Proposal.Create(input.CustomerId, input.RequestedAmount, input.TermMonths, rate);

        proposal.AddTimelineEntry(null, proposal.Status);

        await repository.AddAsync(proposal, ct);
        await repository.SaveChangesAsync(ct);

        return proposal.Id;
    }
}
