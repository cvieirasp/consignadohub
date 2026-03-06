using System.Text;
using System.Text.Json;
using ConsignadoHub.BuildingBlocks.Messaging;
using RabbitMQ.Client;

namespace WorkflowWorker.IntegrationTests.Infrastructure;

/// <summary>
/// Low-level RabbitMQ helper for integration tests.
///
/// Responsibilities:
///   • Publish integration events directly to the topic exchange (bypassing the Outbox)
///   • Declare short-lived test queues bound to specific output routing keys
///   • Poll those queues to assert that expected events were published by the worker
///
/// The serialization format (UTF-8 JSON, runtime type) intentionally mirrors
/// RabbitMqEventPublisher so that the worker's consumers can deserialize the
/// messages produced by this helper, and vice-versa.
/// </summary>
public sealed class RabbitMqTestHelper : IAsyncDisposable
{
    private const string ExchangeName = "consignadohub.events";

    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqTestHelper(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqTestHelper> CreateAsync(
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
        var channel    = await connection.CreateChannelAsync();

        // Declare the exchange (idempotent — workers already declared it, but we ensure
        // it exists before any test queue is bound to it).
        await channel.ExchangeDeclareAsync(
            exchange:   ExchangeName,
            type:       "topic",
            durable:    true,
            autoDelete: false);

        return new RabbitMqTestHelper(connection, channel);
    }

    /// <summary>
    /// Declares a fresh, unnamed test queue and binds it to the exchange with
    /// <paramref name="routingKey"/>. Must be called BEFORE publishing the
    /// triggering event so no output messages are missed.
    /// </summary>
    public async Task<string> DeclareTestQueueAsync(string routingKey)
    {
        var queueName = $"test.{Guid.NewGuid():N}";

        await _channel.QueueDeclareAsync(
            queue:      queueName,
            durable:    false,
            exclusive:  false,
            autoDelete: false);

        await _channel.QueueBindAsync(
            queue:      queueName,
            exchange:   ExchangeName,
            routingKey: routingKey);

        return queueName;
    }

    /// <summary>
    /// Publishes <paramref name="event"/> to the exchange using the same
    /// serialization as RabbitMqEventPublisher (runtime-type UTF-8 JSON,
    /// persistent delivery, application/json content type).
    /// </summary>
    public async Task PublishAsync<T>(T @event, string routingKey)
        where T : IIntegrationEvent
    {
        using var channel = await _connection.CreateChannelAsync();

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

    /// <summary>
    /// Polls <paramref name="queueName"/> until a message arrives or the
    /// timeout elapses. Returns <c>null</c> on timeout (use this to assert
    /// that NO event was published).
    /// </summary>
    public async Task<T?> ConsumeAsync<T>(string queueName, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(8));

        while (DateTime.UtcNow < deadline)
        {
            var result = await _channel.BasicGetAsync(queue: queueName, autoAck: true);

            if (result is not null)
            {
                var body = Encoding.UTF8.GetString(result.Body.Span);
                return JsonSerializer.Deserialize<T>(body);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return default;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel.IsOpen)
            await _channel.CloseAsync();
        _channel.Dispose();

        if (_connection.IsOpen)
            await _connection.CloseAsync();
        _connection.Dispose();
    }
}
