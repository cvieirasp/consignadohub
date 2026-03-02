using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

internal sealed class InboxRepository(NotificationDbContext db) : IInboxRepository
{
    public async Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken ct = default) =>
        await db.InboxMessages.AnyAsync(
            m => m.EventId == eventId && m.ConsumerName == consumerName, ct);

    public async Task AddAsync(InboxMessage message, CancellationToken ct = default) =>
        await db.InboxMessages.AddAsync(message, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
