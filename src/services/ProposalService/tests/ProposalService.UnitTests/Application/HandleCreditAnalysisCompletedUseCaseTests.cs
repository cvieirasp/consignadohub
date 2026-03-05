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

public sealed class HandleCreditAnalysisCompletedUseCaseTests
{
    private readonly Mock<IProposalRepository> _proposalRepositoryMock = new();
    private readonly Mock<IInboxRepository> _inboxRepositoryMock = new();
    private readonly HandleCreditAnalysisCompletedUseCase _useCase;

    public HandleCreditAnalysisCompletedUseCaseTests()
    {
        _useCase = new HandleCreditAnalysisCompletedUseCase(
            _proposalRepositoryMock.Object,
            _inboxRepositoryMock.Object);
    }

    private static CreditAnalysisCompletedEvent ApprovedEvent(Guid proposalId) =>
        new(proposalId, Approved: true, Score: 750, Reason: "Credit approved. Score: 750.");

    private static CreditAnalysisCompletedEvent RejectedEvent(Guid proposalId) =>
        new(proposalId, Approved: false, Score: 450, Reason: "Credit denied. Score: 450.");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldApproveProposal_WhenEventIndicatesApproval()
    {
        var proposalId = Guid.NewGuid();
        var proposal = CreateSubmittedProposal(proposalId);

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

        var result = await _useCase.ExecuteAsync(ApprovedEvent(proposalId));

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(ProposalStatus.Approved);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldRejectProposal_WhenEventIndicatesRejection()
    {
        var proposalId = Guid.NewGuid();
        var proposal = CreateSubmittedProposal(proposalId);

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

        var result = await _useCase.ExecuteAsync(RejectedEvent(proposalId));

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(ProposalStatus.Rejected);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldReturnSuccess_WhenEventAlreadyProcessed()
    {
        var @event = ApprovedEvent(Guid.NewGuid());

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
        var @event = ApprovedEvent(Guid.NewGuid());

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

    private static Proposal CreateSubmittedProposal(Guid id)
    {
        var proposal = Proposal.Create(Guid.NewGuid(), 10_000m, 12, 1.8m);
        // Access the Id field via reflection since it's set privately
        typeof(Proposal)
            .GetProperty("Id")!
            .SetValue(proposal, id);
        return proposal;
    }
}
