using Microsoft.EntityFrameworkCore;
using ProposalService.Application.Exceptions;
using ProposalService.Application.Ports;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;
using ProposalService.Infrastructure.Persistence;

namespace ProposalService.Infrastructure.Repositories;

internal sealed class ProposalRepository(ProposalDbContext db) : IProposalRepository
{
    public async Task<Proposal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Proposals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Proposal?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default) =>
        await db.Proposals
            .Include(p => p.Timeline)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

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

    /// <summary>
    /// Saves changes to the database, handling concurrency exceptions and throwing a 
    /// domain-specific exception if a conflict occurs.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ProposalConcurrencyException">Thrown when a concurrency conflict occurs.</exception>
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ProposalConcurrencyException(
                "Proposal update failed due to a concurrency conflict.",
                ex);
        }
    }
}
