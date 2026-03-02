namespace ConsignadoHub.BuildingBlocks.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default)
        where T : IIntegrationEvent;

    Task PublishRawAsync(string payload, string routingKey, CancellationToken ct = default);
}
