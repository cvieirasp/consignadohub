using System.Text.Json;

namespace ConsignadoHub.BuildingBlocks.Messaging;

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

    public void MarkProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailure(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
