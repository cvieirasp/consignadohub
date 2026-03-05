using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using WorkflowWorker.Contracts.Events;
using FluentAssertions;
using Moq;
using ProposalService.Application.Ports;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;

namespace ProposalService.UnitTests.Application;

public sealed class HandleDisbursementCompletedUseCaseTests
{
    private readonly Mock<IProposalRepository> _proposalRepositoryMock = new();
    private readonly Mock<IInboxRepository> _inboxRepositoryMock = new();
    private readonly HandleDisbursementCompletedUseCase _useCase;

    public HandleDisbursementCompletedUseCaseTests()
    {
        _useCase = new HandleDisbursementCompletedUseCase(
            _proposalRepositoryMock.Object,
            _inboxRepositoryMock.Object);
    }

    private static DisbursementCompletedEvent MakeEvent(Guid proposalId) =>
        new(proposalId, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldTransitionToDisbursed_WhenProposalIsContractGenerated()
    {
        var proposalId = Guid.NewGuid();
        var proposal = CreateContractGeneratedProposal(proposalId);

        _inboxRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _proposalRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _inboxRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _proposalRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _useCase.ExecuteAsync(MakeEvent(proposalId));

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(ProposalStatus.Disbursed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldReturnSuccess_WhenEventAlreadyProcessed()
    {
        var @event = MakeEvent(Guid.NewGuid());

        _inboxRepositoryMock
            .Setup(r => r.ExistsAsync(@event.EventId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _useCase.ExecuteAsync(@event);

        result.IsSuccess.Should().BeTrue();
        _proposalRepositoryMock.Verify(
            r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WhenProposalNotFound()
    {
        var @event = MakeEvent(Guid.NewGuid());

        _inboxRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _proposalRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(@event.ProposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proposal?)null);

        var result = await _useCase.ExecuteAsync(@event);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.NotFound");
    }

    private static Proposal CreateContractGeneratedProposal(Guid id)
    {
        var proposal = Proposal.Create(Guid.NewGuid(), 10_000m, 12, 1.8m);
        typeof(Proposal)
            .GetProperty("Id")!
            .SetValue(proposal, id);
        proposal.UpdateStatus(ProposalStatus.Approved, "Score: 750");
        proposal.UpdateStatus(ProposalStatus.ContractGenerated, "Contract generated.");
        return proposal;
    }
}
