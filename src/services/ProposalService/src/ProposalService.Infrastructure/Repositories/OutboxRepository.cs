using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using ProposalService.Infrastructure.Persistence;

namespace ProposalService.Infrastructure.Repositories;

internal sealed class OutboxRepository(ProposalDbContext db) : IOutboxRepository
{
    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize, CancellationToken ct = default)
    {
        return await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default) =>
        await db.OutboxMessages.AddAsync(message, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
