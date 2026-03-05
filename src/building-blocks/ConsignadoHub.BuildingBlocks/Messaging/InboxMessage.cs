namespace ConsignadoHub.BuildingBlocks.Messaging;

public sealed class InboxMessage
{
    public Guid EventId { get; private set; }
    public string ConsumerName { get; private set; } = default!;
    public DateTimeOffset ProcessedAt { get; private set; }

    private InboxMessage() { }

    /// <summary>
    /// Creates a new InboxMessage to track the processing of an incoming event by a specific consumer. 
    /// This helps ensure idempotency and allows us to track which events have been processed by which consumers.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="consumerName">The name of the consumer processing the event.</param>
    /// <returns>A new instance of InboxMessage.</returns>
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
