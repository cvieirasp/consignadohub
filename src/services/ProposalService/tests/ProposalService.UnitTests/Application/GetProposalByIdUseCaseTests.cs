using FluentAssertions;
using Moq;
using ProposalService.Application.Ports;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Entities;

namespace ProposalService.UnitTests.Application;

public sealed class GetProposalByIdUseCaseTests
{
    private readonly Mock<IProposalRepository> _repositoryMock = new();
    private readonly GetProposalByIdUseCase _useCase;

    public GetProposalByIdUseCaseTests()
    {
        _useCase = new GetProposalByIdUseCase(_repositoryMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenProposalDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proposal?)null);

        var result = await _useCase.ExecuteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.NotFound");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnProposalDto_WhenProposalExists()
    {
        var customerId = Guid.NewGuid();
        var proposal = Proposal.Create(customerId, 10_000m, 12, 1.8m);
        _repositoryMock
            .Setup(r => r.GetByIdAsync(proposal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        var result = await _useCase.ExecuteAsync(proposal.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(proposal.Id);
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.RequestedAmount.Should().Be(10_000m);
        result.Value.TermMonths.Should().Be(12);
        result.Value.MonthlyRate.Should().Be(1.8m);
        result.Value.InstallmentAmount.Should().Be(proposal.InstallmentAmount);
        result.Value.TotalAmount.Should().Be(proposal.TotalAmount);
        result.Value.CET.Should().Be(proposal.CET);
        result.Value.Status.Should().Be(proposal.Status);
        result.Value.CreatedAt.Should().Be(proposal.CreatedAt);
    }
}
