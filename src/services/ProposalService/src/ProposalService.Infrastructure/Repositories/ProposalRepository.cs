using Microsoft.EntityFrameworkCore;
using ProposalService.Application.Ports;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;
using ProposalService.Infrastructure.Persistence;

namespace ProposalService.Infrastructure.Repositories;

internal sealed class ProposalRepository(ProposalDbContext db) : IProposalRepository
{
    public async Task<Proposal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Proposals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Proposal?> GetByIdWithTimelineAsync(Guid id, CancellationToken ct = default) =>
        await db.Proposals
            .AsNoTracking()
            .Include(p => p.Timeline)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Proposal> Items, int TotalCount)> ListByCustomerAsync(
        Guid customerId, ProposalStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Proposals.AsNoTracking().Where(p => p.CustomerId == customerId);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Proposal proposal, CancellationToken ct = default) =>
        await db.Proposals.AddAsync(proposal, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
