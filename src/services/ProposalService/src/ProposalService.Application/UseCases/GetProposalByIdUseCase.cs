using ConsignadoHub.BuildingBlocks.Results;
using ProposalService.Application.DTOs;
using ProposalService.Application.Ports;
using ProposalService.Domain.Errors;

namespace ProposalService.Application.UseCases;

public sealed class GetProposalByIdUseCase(IProposalRepository repository)
{
    public async Task<Result<ProposalDto>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var proposal = await repository.GetByIdAsync(id, ct);
        if (proposal is null)
            return ProposalErrors.NotFound(id);

        return new ProposalDto(
            proposal.Id,
            proposal.CustomerId,
            proposal.RequestedAmount,
            proposal.TermMonths,
            proposal.MonthlyRate,
            proposal.InstallmentAmount,
            proposal.TotalAmount,
            proposal.CET,
            proposal.Status,
            proposal.CreatedAt);
    }
}
