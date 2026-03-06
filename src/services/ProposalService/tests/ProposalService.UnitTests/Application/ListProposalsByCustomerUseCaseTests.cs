using FluentAssertions;
using Moq;
using ProposalService.Application.DTOs;
using ProposalService.Application.Ports;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;

namespace ProposalService.UnitTests.Application;

public sealed class ListProposalsByCustomerUseCaseTests
{
    private readonly Mock<IProposalRepository> _repositoryMock = new();
    private readonly ListProposalsByCustomerUseCase _useCase;

    public ListProposalsByCustomerUseCaseTests()
    {
        _useCase = new ListProposalsByCustomerUseCase(_repositoryMock.Object);
    }

    private static ListProposalsByCustomerInput ValidInput(
        Guid? customerId = null,
        ProposalStatus? status = null,
        int page = 1,
        int pageSize = 20) =>
        new(customerId ?? Guid.NewGuid(), status, page, pageSize);

    private void SetupRepository(
        Guid customerId,
        ProposalStatus? status,
        IReadOnlyList<Proposal> items,
        int total)
    {
        _repositoryMock
            .Setup(r => r.ListByCustomerAsync(
                customerId, status, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, total));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnMappedDtos_WhenProposalsExist()
    {
        var customerId = Guid.NewGuid();
        var proposal = Proposal.Create(customerId, 10_000m, 12, 1.8m);
        SetupRepository(customerId, null, [proposal], 1);

        var result = await _useCase.ExecuteAsync(ValidInput(customerId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);

        var dto = result.Value.Items[0];
        dto.Id.Should().Be(proposal.Id);
        dto.CustomerId.Should().Be(customerId);
        dto.RequestedAmount.Should().Be(10_000m);
        dto.TermMonths.Should().Be(12);
        dto.Status.Should().Be(proposal.Status);
        dto.CreatedAt.Should().Be(proposal.CreatedAt);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenNoProposalsExist()
    {
        var customerId = Guid.NewGuid();
        SetupRepository(customerId, null, [], 0);

        var result = await _useCase.ExecuteAsync(ValidInput(customerId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldPassStatusFilter_ToRepository()
    {
        var customerId = Guid.NewGuid();
        SetupRepository(customerId, ProposalStatus.Approved, [], 0);

        await _useCase.ExecuteAsync(ValidInput(customerId, status: ProposalStatus.Approved));

        _repositoryMock.Verify(r => r.ListByCustomerAsync(
            customerId, ProposalStatus.Approved, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task ExecuteAsync_ShouldNormalizePage(int inputPage, int expectedPage)
    {
        var customerId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.ListByCustomerAsync(
                customerId, null, expectedPage, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Proposal>(), 0));

        await _useCase.ExecuteAsync(ValidInput(customerId, page: inputPage));

        _repositoryMock.Verify(r => r.ListByCustomerAsync(
            customerId, null, expectedPage, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0, 1)]
    [InlineData(200, 100)]
    [InlineData(50, 50)]
    public async Task ExecuteAsync_ShouldClampPageSize(int inputPageSize, int expectedPageSize)
    {
        var customerId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.ListByCustomerAsync(
                customerId, null, It.IsAny<int>(), expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Proposal>(), 0));

        await _useCase.ExecuteAsync(ValidInput(customerId, pageSize: inputPageSize));

        _repositoryMock.Verify(r => r.ListByCustomerAsync(
            customerId, null, It.IsAny<int>(), expectedPageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ShouldReturnCorrectPaginationMetadata()
    {
        var customerId = Guid.NewGuid();
        SetupRepository(customerId, null, [], 45);

        var result = await _useCase.ExecuteAsync(ValidInput(customerId, page: 2, pageSize: 20));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(45);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }
}
