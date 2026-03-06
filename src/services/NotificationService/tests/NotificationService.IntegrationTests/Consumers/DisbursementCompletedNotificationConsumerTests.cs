using WorkflowWorker.Contracts.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Persistence;
using NotificationService.IntegrationTests.Infrastructure;

namespace NotificationService.IntegrationTests.Consumers;

/// <summary>
/// Integration tests for <c>DisbursementCompletedNotificationConsumer</c>.
/// </summary>
[Trait("Category", "Integration")]
[Collection("NotificationService")]
public class DisbursementCompletedNotificationConsumerTests(NotificationServiceFactory factory)
    : IAsyncLifetime
{
    private const string ConsumerName    = "NotificationDisbursementCompletedConsumer";
    private const string InputRoutingKey = "disbursement.completed";

    private RabbitMqTestPublisher _publisher = null!;

    public async Task InitializeAsync() =>
        _publisher = await RabbitMqTestPublisher.CreateAsync(
            factory.RabbitMqHostname, factory.RabbitMqAmqpPort,
            factory.RabbitMqUsername, factory.RabbitMqPassword);

    public async Task DisposeAsync() =>
        await _publisher.DisposeAsync();

    [Fact]
    public async Task Handle_ShouldRecordInboxMessage()
    {
        // Arrange
        var @event = new DisbursementCompletedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow)
        {
            CorrelationId = Guid.NewGuid().ToString(),
        };

        // Act
        await _publisher.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var entry = await db.InboxMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.EventId == @event.EventId && m.ConsumerName == ConsumerName);

        entry.Should().NotBeNull();
        entry!.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenSameEventReceivedTwice()
    {
        // Arrange
        var @event = new DisbursementCompletedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        // Act — first delivery
        await _publisher.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Act — duplicate delivery
        await _publisher.PublishAsync(@event, InputRoutingKey);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var count = await db.InboxMessages
            .AsNoTracking()
            .CountAsync(m => m.EventId == @event.EventId && m.ConsumerName == ConsumerName);

        count.Should().Be(1);
    }
}
