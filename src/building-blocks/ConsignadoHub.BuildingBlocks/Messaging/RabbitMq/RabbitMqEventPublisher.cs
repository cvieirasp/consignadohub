using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using RabbitMQ.Client;

namespace ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly RabbitMqSettings _settings;
    private int _disposed;

    private RabbitMqEventPublisher(IConnection connection, RabbitMqSettings settings)
    {
        _connection = connection;
        _settings = settings;
    }

    public static async Task<RabbitMqEventPublisher> CreateAsync(RabbitMqSettings settings)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.Username,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
        };

        var connection = await factory.CreateConnectionAsync();
        return new RabbitMqEventPublisher(connection, settings);
    }

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default)
        where T : IIntegrationEvent
    {
        var payload = JsonSerializer.Serialize(@event, @event.GetType());
        await PublishRawAsync(payload, routingKey, ct);
    }

    public async Task PublishRawAsync(string payload, string routingKey, CancellationToken ct = default)
    {
        using var activity = MessagingActivitySource.Source.StartActivity(
            $"{_settings.ExchangeName} {routingKey} publish",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", _settings.ExchangeName);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("messaging.rabbitmq.routing_key", routingKey);

        using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: "topic",
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(payload);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
        };

        await channel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    public IConnection GetConnection() => _connection;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_connection.IsOpen)
            await _connection.CloseAsync();
        _connection.Dispose();
    }
}
