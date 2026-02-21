using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;

namespace ProposalService.Application.Ports;

public interface IProposalRepository
{
    Task<Proposal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Proposal?> GetByIdWithTimelineAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Proposal> Items, int TotalCount)> ListByCustomerAsync(
        Guid customerId, ProposalStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Proposal proposal, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
