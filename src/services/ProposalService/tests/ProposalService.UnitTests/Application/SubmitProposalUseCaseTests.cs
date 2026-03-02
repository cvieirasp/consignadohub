using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Outbox;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ProposalService.Application.Configuration;
using ProposalService.Application.DTOs;
using ProposalService.Application.Ports;
using ProposalService.Application.UseCases;
using ProposalService.Application.Validators;
using ProposalService.Domain.Entities;

namespace ProposalService.UnitTests.Application;

public sealed class SubmitProposalUseCaseTests
{
    private readonly Mock<IProposalRepository> _repositoryMock = new();
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock = new();
    private readonly SubmitProposalUseCase _useCase;

    public SubmitProposalUseCaseTests()
    {
        var settings = Options.Create(new ProposalSettings { DefaultMonthlyRate = 1.8m });
        _useCase = new SubmitProposalUseCase(
            _repositoryMock.Object,
            _outboxRepositoryMock.Object,
            new SubmitProposalValidator(),
            settings);
    }

    private static SubmitProposalInput ValidInput() => new(
        Guid.NewGuid(),
        10_000m,
        12);

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldReturnGuid_ForValidInput()
    {
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _useCase.ExecuteAsync(ValidInput());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldCreateOutboxMessage_ForValidInput()
    {
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _useCase.ExecuteAsync(ValidInput());

        _outboxRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<OutboxMessage>(m => m.RoutingKey == "proposal.submitted"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WhenAmountBelowMinimum()
    {
        var input = ValidInput() with { RequestedAmount = 50m };

        var result = await _useCase.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WhenTermBelowMinimum()
    {
        var input = ValidInput() with { TermMonths = 3 };

        var result = await _useCase.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.Validation");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WhenCustomerIdIsEmpty()
    {
        var input = ValidInput() with { CustomerId = Guid.Empty };

        var result = await _useCase.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
    }
}
