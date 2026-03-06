using FluentAssertions;
using Moq;
using ProposalService.Application.Ports;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;

namespace ProposalService.UnitTests.Application;

public sealed class GetProposalTimelineUseCaseTests
{
    private readonly Mock<IProposalRepository> _repositoryMock = new();
    private readonly GetProposalTimelineUseCase _useCase;

    public GetProposalTimelineUseCaseTests()
    {
        _useCase = new GetProposalTimelineUseCase(_repositoryMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenProposalDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdWithTimelineAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proposal?)null);

        var result = await _useCase.ExecuteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.NotFound");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenTimelineIsEmpty()
    {
        var proposal = Proposal.Create(Guid.NewGuid(), 10_000m, 12, 1.8m);
        _repositoryMock
            .Setup(r => r.GetByIdWithTimelineAsync(proposal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        var result = await _useCase.ExecuteAsync(proposal.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnMappedEntries_WhenTimelineHasEntries()
    {
        var proposal = Proposal.Create(Guid.NewGuid(), 10_000m, 12, 1.8m);
        proposal.UpdateStatus(ProposalStatus.UnderAnalysis);
        proposal.UpdateStatus(ProposalStatus.Approved, "Credit approved");

        _repositoryMock
            .Setup(r => r.GetByIdWithTimelineAsync(proposal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        var result = await _useCase.ExecuteAsync(proposal.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);

        var first = result.Value[0];
        first.Id.Should().NotBeEmpty();
        first.FromStatus.Should().Be(ProposalStatus.Submitted);
        first.ToStatus.Should().Be(ProposalStatus.UnderAnalysis);
        first.Reason.Should().BeNull();

        var second = result.Value[1];
        second.Id.Should().NotBeEmpty();
        second.FromStatus.Should().Be(ProposalStatus.UnderAnalysis);
        second.ToStatus.Should().Be(ProposalStatus.Approved);
        second.Reason.Should().Be("Credit approved");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnEntriesOrderedByOccurredAt()
    {
        var proposal = Proposal.Create(Guid.NewGuid(), 10_000m, 12, 1.8m);
        proposal.UpdateStatus(ProposalStatus.UnderAnalysis);
        proposal.UpdateStatus(ProposalStatus.Approved);
        proposal.UpdateStatus(ProposalStatus.Disbursed);

        _repositoryMock
            .Setup(r => r.GetByIdWithTimelineAsync(proposal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        var result = await _useCase.ExecuteAsync(proposal.Id);

        result.IsSuccess.Should().BeTrue();
        var entries = result.Value!;
        entries.Should().HaveCount(3);
        entries.Should().BeInAscendingOrder(e => e.OccurredAt);
    }
}
