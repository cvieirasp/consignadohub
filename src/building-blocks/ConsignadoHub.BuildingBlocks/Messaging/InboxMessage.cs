namespace ConsignadoHub.BuildingBlocks.Messaging;

public sealed class InboxMessage
{
    public Guid EventId { get; private set; }
    public string ConsumerName { get; private set; } = default!;
    public DateTimeOffset ProcessedAt { get; private set; }

    private InboxMessage() { }

    public static InboxMessage Create(Guid eventId, string consumerName)
    {
        return new InboxMessage
        {
            EventId = eventId,
            ConsumerName = consumerName,
            ProcessedAt = DateTimeOffset.UtcNow,
        };
    }
}
