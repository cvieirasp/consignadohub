using System.Text;
using System.Text.Json;
using ConsignadoHub.BuildingBlocks.Messaging;
using RabbitMQ.Client;

namespace NotificationService.IntegrationTests.Infrastructure;

/// <summary>
/// Minimal RabbitMQ helper for NotificationService integration tests.
///
/// NotificationService is a pure fan-out consumer — it never publishes output events,
/// so only <see cref="PublishAsync{T}"/> is needed. The serialization format intentionally
/// mirrors <c>RabbitMqEventPublisher</c> (runtime-type UTF-8 JSON, persistent delivery).
/// </summary>
public sealed class RabbitMqTestPublisher : IAsyncDisposable
{
    private const string ExchangeName = "consignadohub.events";

    private readonly IConnection _connection;

    private RabbitMqTestPublisher(IConnection connection) =>
        _connection = connection;

    public static async Task<RabbitMqTestPublisher> CreateAsync(
        string hostname, int port, string username, string password)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostname,
            Port     = port,
            UserName = username,
            Password = password,
        };

        var connection = await factory.CreateConnectionAsync();
        return new RabbitMqTestPublisher(connection);
    }

    /// <summary>
    /// Publishes <paramref name="event"/> to the topic exchange using the same
    /// format as <c>RabbitMqEventPublisher</c>: runtime-type JSON encoded as UTF-8,
    /// persistent delivery mode.
    /// </summary>
    public async Task PublishAsync<T>(T @event, string routingKey)
        where T : IIntegrationEvent
    {
        using var channel = await _connection.CreateChannelAsync();

        // Declare exchange (idempotent — workers already declared it)
        await channel.ExchangeDeclareAsync(
            exchange:   ExchangeName,
            type:       "topic",
            durable:    true,
            autoDelete: false);

        var payload = JsonSerializer.Serialize(@event, @event.GetType());
        var body    = Encoding.UTF8.GetBytes(payload);

        var props = new BasicProperties
        {
            ContentType  = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
        };

        await channel.BasicPublishAsync(
            exchange:        ExchangeName,
            routingKey:      routingKey,
            mandatory:       false,
            basicProperties: props,
            body:            body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsOpen)
            await _connection.CloseAsync();
        _connection.Dispose();
    }
}
