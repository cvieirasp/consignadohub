using ConsignadoHub.BuildingBlocks.Results;
using ProposalService.Application.DTOs;
using ProposalService.Application.Ports;

namespace ProposalService.Application.UseCases;

public sealed class ListProposalsByCustomerUseCase(IProposalRepository repository)
{
    public async Task<Result<PagedResult<ProposalSummaryDto>>> ExecuteAsync(
        ListProposalsByCustomerInput input, CancellationToken ct = default)
    {
        var page = Math.Max(1, input.Page);
        var pageSize = Math.Clamp(input.PageSize, 1, 100);

        var (items, total) = await repository.ListByCustomerAsync(
            input.CustomerId, input.Status, page, pageSize, ct);

        var dtos = items.Select(p => new ProposalSummaryDto(
            p.Id, p.CustomerId, p.RequestedAmount, p.TermMonths, p.Status, p.CreatedAt)).ToList();

        return new PagedResult<ProposalSummaryDto>(dtos, total, page, pageSize);
    }
}
