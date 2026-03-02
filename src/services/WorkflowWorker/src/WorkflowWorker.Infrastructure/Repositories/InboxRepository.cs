using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using WorkflowWorker.Infrastructure.Persistence;

namespace WorkflowWorker.Infrastructure.Repositories;

internal sealed class InboxRepository(WorkflowDbContext db) : IInboxRepository
{
    public async Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken ct = default) =>
        await db.InboxMessages.AnyAsync(
            m => m.EventId == eventId && m.ConsumerName == consumerName, ct);

    public async Task AddAsync(InboxMessage message, CancellationToken ct = default) =>
        await db.InboxMessages.AddAsync(message, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
