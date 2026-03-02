namespace ConsignadoHub.BuildingBlocks.Messaging.Outbox;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(int batchSize, CancellationToken ct = default);
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
