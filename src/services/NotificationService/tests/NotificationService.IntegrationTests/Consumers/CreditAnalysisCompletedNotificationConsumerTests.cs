using WorkflowWorker.Contracts.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Persistence;
using NotificationService.IntegrationTests.Infrastructure;

namespace NotificationService.IntegrationTests.Consumers;

/// <summary>
/// Integration tests for <c>CreditAnalysisCompletedNotificationConsumer</c>.
/// Verifies inbox persistence for both approved and rejected outcomes.
/// </summary>
[Trait("Category", "Integration")]
[Collection("NotificationService")]
public class CreditAnalysisCompletedNotificationConsumerTests(NotificationServiceFactory factory)
    : IAsyncLifetime
{
    private const string ConsumerName    = "NotificationCreditAnalysisCompletedConsumer";
    private const string InputRoutingKey = "proposal.credit.completed";

    private RabbitMqTestPublisher _publisher = null!;

    public async Task InitializeAsync() =>
        _publisher = await RabbitMqTestPublisher.CreateAsync(
            factory.RabbitMqHostname, factory.RabbitMqAmqpPort,
            factory.RabbitMqUsername, factory.RabbitMqPassword);

    public async Task DisposeAsync() =>
        await _publisher.DisposeAsync();

    [Fact]
    public async Task Handle_WhenApproved_ShouldRecordInboxMessage()
    {
        // Arrange
        var @event = new CreditAnalysisCompletedEvent(
            Guid.NewGuid(),
            Approved: true,
            Score: 720,
            Reason: "Score sufficient")
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
    public async Task Handle_WhenRejected_ShouldRecordInboxMessage()
    {
        // Arrange
        var @event = new CreditAnalysisCompletedEvent(
            Guid.NewGuid(),
            Approved: false,
            Score: 380,
            Reason: "Score below threshold")
        {
            CorrelationId = Guid.NewGuid().ToString(),
        };

        // Act
        await _publisher.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — inbox is recorded regardless of approval outcome
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var entry = await db.InboxMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.EventId == @event.EventId && m.ConsumerName == ConsumerName);

        entry.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenSameEventReceivedTwice()
    {
        // Arrange
        var @event = new CreditAnalysisCompletedEvent(
            Guid.NewGuid(),
            Approved: true,
            Score: 800,
            Reason: "Score sufficient");

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
