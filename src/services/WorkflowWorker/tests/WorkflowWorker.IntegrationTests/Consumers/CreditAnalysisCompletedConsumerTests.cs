using WorkflowWorker.Contracts.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.IntegrationTests.Infrastructure;

namespace WorkflowWorker.IntegrationTests.Consumers;

/// <summary>
/// Integration tests for <c>CreditAnalysisCompletedConsumer</c> (WorkflowWorker).
///
/// Covers:
///   • Approved path → ContractGeneratedEvent published
///   • Rejected path → ContractGeneratedEvent NOT published
///   • Idempotency → only one InboxMessages row per EventId
/// </summary>
[Trait("Category", "Integration")]
public class CreditAnalysisCompletedConsumerTests(WorkflowWorkerFactory factory)
    : IClassFixture<WorkflowWorkerFactory>, IAsyncLifetime
{
    private const string ConsumerName     = "WorkflowCreditAnalysisCompletedConsumer";
    private const string InputRoutingKey  = "proposal.credit.completed";
    private const string OutputRoutingKey = "contract.generated";

    private RabbitMqTestHelper _helper = null!;

    public async Task InitializeAsync() =>
        _helper = await RabbitMqTestHelper.CreateAsync(factory.RabbitMqHostname, factory.RabbitMqAmqpPort, factory.RabbitMqUsername, factory.RabbitMqPassword);

    public async Task DisposeAsync() =>
        await _helper.DisposeAsync();

    [Fact]
    public async Task Handle_WhenApproved_ShouldPublishContractGeneratedEvent()
    {
        // Arrange
        var proposalId = Guid.NewGuid();
        var @event = new CreditAnalysisCompletedEvent(
            proposalId,
            Approved: true,
            Score: 750,
            Reason: "Score sufficient")
        {
            CorrelationId = Guid.NewGuid().ToString(),
        };

        var outputQueue = await _helper.DeclareTestQueueAsync(OutputRoutingKey);

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — ContractGeneratedEvent published
        var result = await _helper.ConsumeAsync<ContractGeneratedEvent>(outputQueue);

        result.Should().NotBeNull();
        result!.ProposalId.Should().Be(proposalId);
        result.ContractId.Should().NotBe(Guid.Empty);
        result.ContractUrl.Should().NotBeNullOrWhiteSpace()
            .And.EndWith(".pdf");
        result.CorrelationId.Should().Be(@event.CorrelationId);
    }

    [Fact]
    public async Task Handle_WhenRejected_ShouldNotPublishContractGeneratedEvent()
    {
        // Arrange
        var @event = new CreditAnalysisCompletedEvent(
            Guid.NewGuid(),
            Approved: false,
            Score: 400,
            Reason: "Score below threshold");

        var outputQueue = await _helper.DeclareTestQueueAsync(OutputRoutingKey);

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);

        // Consumer still records the inbox entry even for rejected proposals.
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — no ContractGeneratedEvent published (short timeout to avoid long waits)
        var result = await _helper.ConsumeAsync<ContractGeneratedEvent>(
            outputQueue, TimeSpan.FromSeconds(2));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenRejected_ShouldRecordInboxMessage()
    {
        // Arrange
        var @event = new CreditAnalysisCompletedEvent(
            Guid.NewGuid(),
            Approved: false,
            Score: 350,
            Reason: "Score below threshold");

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — inbox entry persisted even for rejected proposals
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowWorker.Infrastructure.Persistence.WorkflowDbContext>();

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
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Act — duplicate delivery
        await _helper.PublishAsync(@event, InputRoutingKey);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert — exactly one inbox entry
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowWorker.Infrastructure.Persistence.WorkflowDbContext>();

        var count = await db.InboxMessages
            .AsNoTracking()
            .CountAsync(m => m.EventId == @event.EventId && m.ConsumerName == ConsumerName);

        count.Should().Be(1);
    }
}
