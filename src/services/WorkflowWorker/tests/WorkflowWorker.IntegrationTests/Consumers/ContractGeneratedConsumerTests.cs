using WorkflowWorker.Contracts.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.IntegrationTests.Infrastructure;

namespace WorkflowWorker.IntegrationTests.Consumers;

/// <summary>
/// Integration tests for <c>ContractGeneratedConsumer</c> (WorkflowWorker).
///
/// Covers:
///   • Happy path → DisbursementCompletedEvent published with correct payload
///   • CorrelationId propagation
///   • Idempotency → only one InboxMessages row per EventId
/// </summary>
[Trait("Category", "Integration")]
public class ContractGeneratedConsumerTests(WorkflowWorkerFactory factory)
    : IClassFixture<WorkflowWorkerFactory>, IAsyncLifetime
{
    private const string ConsumerName     = "WorkflowContractGeneratedConsumer";
    private const string InputRoutingKey  = "contract.generated";
    private const string OutputRoutingKey = "disbursement.completed";

    private RabbitMqTestHelper _helper = null!;

    public async Task InitializeAsync() =>
        _helper = await RabbitMqTestHelper.CreateAsync(factory.RabbitMqHostname, factory.RabbitMqAmqpPort);

    public async Task DisposeAsync() =>
        await _helper.DisposeAsync();

    [Fact]
    public async Task Handle_ShouldPublishDisbursementCompletedEvent()
    {
        // Arrange
        var proposalId  = Guid.NewGuid();
        var contractId  = Guid.NewGuid();
        var @event = new ContractGeneratedEvent(
            proposalId,
            contractId,
            $"https://contracts.consignadohub.internal/{contractId:N}.pdf")
        {
            CorrelationId = Guid.NewGuid().ToString(),
        };

        var outputQueue = await _helper.DeclareTestQueueAsync(OutputRoutingKey);

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert — DisbursementCompletedEvent published
        var result = await _helper.ConsumeAsync<DisbursementCompletedEvent>(outputQueue);

        result.Should().NotBeNull();
        result!.ProposalId.Should().Be(proposalId);
        result.DisbursementId.Should().NotBe(Guid.Empty);
        result.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        result.CorrelationId.Should().Be(@event.CorrelationId);
    }

    [Fact]
    public async Task Handle_ShouldRecordInboxMessageOnSuccess()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var @event = new ContractGeneratedEvent(
            Guid.NewGuid(),
            contractId,
            $"https://contracts.consignadohub.internal/{contractId:N}.pdf");

        // Act
        await _helper.PublishAsync(@event, InputRoutingKey);
        await factory.WaitForInboxEntryAsync(@event.EventId, ConsumerName);

        // Assert
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
        var contractId = Guid.NewGuid();
        var @event = new ContractGeneratedEvent(
            Guid.NewGuid(),
            contractId,
            $"https://contracts.consignadohub.internal/{contractId:N}.pdf");

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
