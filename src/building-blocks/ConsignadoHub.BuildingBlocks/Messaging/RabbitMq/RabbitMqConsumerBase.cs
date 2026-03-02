using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;

public abstract class RabbitMqConsumerBase<TEvent> : BackgroundService
    where TEvent : IIntegrationEvent
{
    private readonly RabbitMqEventPublisher _publisher;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger _logger;

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }
    protected abstract string ConsumerName { get; }

    protected RabbitMqConsumerBase(
        RabbitMqEventPublisher publisher,
        RabbitMqSettings settings,
        ILogger logger)
    {
        _publisher = publisher;
        _settings = settings;
        _logger = logger;
    }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Consumer} starting on queue '{Queue}'.", ConsumerName, QueueName);

        var connection = _publisher.GetConnection();
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare the exchange
        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: "topic",
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // DLQ
        var dlqName = $"{QueueName}.dlq";
        await channel.QueueDeclareAsync(
            queue: dlqName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Main queue with DLQ binding
        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = dlqName,
        };

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: _settings.ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.Span);

            using var activity = MessagingActivitySource.Source.StartActivity(
                $"{QueueName} process",
                ActivityKind.Consumer);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", QueueName);
            activity?.SetTag("messaging.consumer", ConsumerName);

            try
            {
                var @event = JsonSerializer.Deserialize<TEvent>(body);

                if (@event is null)
                {
                    _logger.LogWarning("{Consumer} received null event from queue '{Queue}'.", ConsumerName, QueueName);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                activity?.SetTag("messaging.message_id", @event.EventId.ToString());
                activity?.SetTag("messaging.correlation_id", @event.CorrelationId.ToString());

                _logger.LogInformation(
                    "Event processing started. Consumer={ConsumerName} EventId={EventId} Queue={Queue}.",
                    ConsumerName, @event.EventId, QueueName);

                await HandleAsync(@event, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "{Consumer} successfully processed event {EventId}.",
                    ConsumerName, @event.EventId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex,
                    "{Consumer} failed to process message from queue '{Queue}'. Sending to DLQ.",
                    ConsumerName, QueueName);

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        // Keep the service running until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _logger.LogInformation("{Consumer} stopped.", ConsumerName);
    }
}
