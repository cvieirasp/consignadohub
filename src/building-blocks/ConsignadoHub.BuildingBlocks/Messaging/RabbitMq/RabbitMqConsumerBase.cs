using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Base class for RabbitMQ consumers. Handles connection setup, queue declaration, 
/// and message processing with error handling and DLQ support.
/// </summary>
/// <typeparam name="TEvent">The type of the integration event.</typeparam>
public abstract class RabbitMqConsumerBase<TEvent>(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    ILogger logger) : BackgroundService
    where TEvent : IIntegrationEvent
{
    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }
    protected abstract string ConsumerName { get; }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken ct);

    /// <summary>
    /// Executes the consumer by setting up the RabbitMQ connection, declaring the necessary queues
    /// and exchanges, and processing incoming messages with proper error handling and DLQ support.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token to stop the consumer.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Consumer} starting on queue '{Queue}'.", ConsumerName, QueueName);

        var connection = publisher.GetConnection();
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare the exchange.
        await channel.ExchangeDeclareAsync(
            exchange: settings.ExchangeName,
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

        // Main queue with DLQ binding.
        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = dlqName,
        };

        // Declare the main queue and bind it to the exchange with the specified routing key.
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: stoppingToken);

        // Bind the queue to the exchange with the routing key.
        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: settings.ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        // Set QoS to process one message at a time.
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        // Handle incoming messages asynchronously.
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.Span);

            // Start an activity for tracing the message processing.
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
                    logger.LogWarning("{Consumer} received null event from queue '{Queue}'.", ConsumerName, QueueName);
                    // Reject the message and send it to the DLQ.
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                activity?.SetTag("messaging.message_id", @event.EventId.ToString());
                activity?.SetTag("messaging.correlation_id", @event.CorrelationId.ToString());

                logger.LogInformation(
                    "Event processing started. Consumer={ConsumerName} EventId={EventId} Queue={Queue}.",
                    ConsumerName, @event.EventId, QueueName);

                await HandleAsync(@event, stoppingToken);
                // Acknowledge the message after successful processing.
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                logger.LogInformation(
                    "{Consumer} successfully processed event {EventId}.",
                    ConsumerName, @event.EventId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                logger.LogError(ex,
                    "{Consumer} failed to process message from queue '{Queue}'. Sending to DLQ.",
                    ConsumerName, QueueName);

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        // Start consuming messages from the queue.
        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        // Keep the service running until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        logger.LogInformation("{Consumer} stopped.", ConsumerName);
    }
}
