using ConsignadoHub.BuildingBlocks.Results;
using Microsoft.Extensions.Options;
using ProposalService.Application.Configuration;
using ProposalService.Application.DTOs;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Errors;

namespace ProposalService.Application.UseCases;

public sealed class SimulateProposalUseCase(IOptions<ProposalSettings> settings)
{
    private readonly ProposalSettings _settings = settings.Value;

    public Result<SimulationResultDto> Execute(SimulateProposalInput input)
    {
        if (input.RequestedAmount < 100m || input.RequestedAmount > 500_000m)
            return ProposalErrors.InvalidAmount;

        if (input.TermMonths < 6 || input.TermMonths > 120)
            return ProposalErrors.InvalidTerm;

        var rate = input.MonthlyRate ?? _settings.DefaultMonthlyRate;
        var (installment, total, cet) = Proposal.CalculateFinancials(input.RequestedAmount, input.TermMonths, rate);

        return new SimulationResultDto(
            input.RequestedAmount,
            input.TermMonths,
            rate,
            installment,
            total,
            cet);
    }
}
