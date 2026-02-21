using ConsignadoHub.BuildingBlocks.Results;
using ProposalService.Application.DTOs;
using ProposalService.Application.Ports;
using ProposalService.Domain.Errors;

namespace ProposalService.Application.UseCases;

public sealed class GetProposalTimelineUseCase(IProposalRepository repository)
{
    public async Task<Result<IReadOnlyList<ProposalTimelineEntryDto>>> ExecuteAsync(
        Guid proposalId, CancellationToken ct = default)
    {
        var proposal = await repository.GetByIdWithTimelineAsync(proposalId, ct);
        if (proposal is null)
            return ProposalErrors.NotFound(proposalId);

        var entries = proposal.Timeline
            .OrderBy(e => e.OccurredAt)
            .Select(e => new ProposalTimelineEntryDto(e.Id, e.FromStatus, e.ToStatus, e.OccurredAt, e.Reason))
            .ToList();

        return entries;
    }
}
