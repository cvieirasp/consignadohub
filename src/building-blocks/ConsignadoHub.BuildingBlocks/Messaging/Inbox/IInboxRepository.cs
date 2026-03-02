namespace ConsignadoHub.BuildingBlocks.Messaging.Inbox;

public interface IInboxRepository
{
    Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken ct = default);
    Task AddAsync(InboxMessage message, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
