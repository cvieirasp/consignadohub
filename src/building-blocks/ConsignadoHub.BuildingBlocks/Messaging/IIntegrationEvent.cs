namespace ConsignadoHub.BuildingBlocks.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string CorrelationId { get; }
}
