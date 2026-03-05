using System.Text.Json;

namespace ConsignadoHub.BuildingBlocks.Messaging;

/// <summary>
/// Represents a message stored in the outbox for reliable event publishing.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public string RoutingKey { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    /// <summary>
    /// Creates a new outbox message from an integration event.
    /// </summary>
    /// <param name="event">The integration event to be stored in the outbox.</param>
    /// <param name="routingKey">The routing key for the message.</param>
    /// <returns>A new instance of OutboxMessage.</returns>
    public static OutboxMessage Create(IIntegrationEvent @event, string routingKey)
    {
        return new OutboxMessage
        {
            Id = @event.EventId,
            EventType = @event.GetType().FullName!,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            RoutingKey = routingKey,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Marks the outbox message as processed by setting the ProcessedAt timestamp.
    /// </summary>
    public void MarkProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Records a failure for the outbox message, incrementing the attempt count and storing the error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    public void RecordFailure(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
