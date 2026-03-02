using ConsignadoHub.Contracts.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.IntegrationTests.Infrastructure;

namespace WorkflowWorker.IntegrationTests.Consumers;

/// <summary>
/// Integration tests for <c>ProposalSubmittedConsumer</c>.
///
/// Each test:
///   1. Declares a test queue bound to the output routing key BEFORE publishing
///      (so no messages are missed due to timing).
///   2. Publishes a <see cref="ProposalSubmittedEvent"/> to the input routing key.
///   3. Polls the InboxMessages table until the consumer records the processed entry.
///   4. Asserts the published output event and inbox state.
/// </summary>
[Trait("Category", "Integration")]
public class ProposalSubmittedConsumerTests(WorkflowWorkerFactory factory)
    : IClassFixture<WorkflowWorkerFactory>, IAsyncLifetime
{
    private const string ConsumerName     = "ProposalSubmittedConsumer";
    private const string InputRoutingKey  = "proposal.submitted";
    private const string OutputRoutingKey = "proposal.credit.completed";

    private RabbitMqTestHelper _helper = null!;

    public async Task InitializeAsync() =>
        _helper = await RabbitMqTestHelper.CreateAsync(factory.RabbitMqHostname, factory.RabbitMqAmqpPort);

    public async Task DisposeAsync() =>
        await _helper.DisposeAsync();

    [Fact]
    public async Task Handle_ShouldPublishCreditAnalysisCompletedEvent()
    {
        // Arrange
        var proposalId = Guid.NewGuid();
        var @event = new ProposalSubmittedEvent(proposalId, Guid.NewGuid(), 15_000m, 24)
        {
            CorrelationId = Guid.NewGuid().ToString(),
        };

        // Bind a test queue BEFORE publishing so we capture the output event.
        var outputQueue = await _helper.DeclareTestQueueAsync(OutputRoutingKey);

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);

        // Wait for the consumer to write its inbox entry (processing completed).
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — output event
        var result = await _helper.ConsumeAsync<CreditAnalysisCompletedEvent>(outputQueue);

        result.Should().NotBeNull();
        result!.ProposalId.Should().Be(proposalId);
        result.Score.Should().BeInRange(300, 900);
        result.Reason.Should().NotBeNullOrWhiteSpace();
        result.CorrelationId.Should().Be(@event.CorrelationId);
    }

    [Fact]
    public async Task Handle_ShouldRecordInboxMessageOnSuccess()
    {
        // Arrange
        var @event = new ProposalSubmittedEvent(Guid.NewGuid(), Guid.NewGuid(), 8_000m, 12);

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — inbox entry persisted
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowWorker.Infrastructure.Persistence.WorkflowDbContext>();

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
        var @event = new ProposalSubmittedEvent(Guid.NewGuid(), Guid.NewGuid(), 10_000m, 12);

        // Act — first delivery
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Act — duplicate delivery (simulates redelivery / at-least-once guarantee)
        await _helper.PublishAsync(@event, InputRoutingKey);
        await Task.Delay(TimeSpan.FromSeconds(2)); // give the consumer time to attempt processing

        // Assert — exactly one inbox entry regardless of how many deliveries occurred
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowWorker.Infrastructure.Persistence.WorkflowDbContext>();

        var count = await db.InboxMessages
            .AsNoTracking()
            .CountAsync(m => m.EventId == @event.EventId && m.ConsumerName == ConsumerName);

        count.Should().Be(1);
    }
}
