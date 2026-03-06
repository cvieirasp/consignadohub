using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.Outbox;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
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
    private readonly Mock<IValidator<SubmitProposalInput>> _alwaysValidMock = new();
    private readonly SubmitProposalUseCase _useCaseWithPassthroughValidator;

    public SubmitProposalUseCaseTests()
    {
        var settings = Options.Create(new ProposalSettings { DefaultMonthlyRate = 1.8m });
        _useCase = new SubmitProposalUseCase(
            _repositoryMock.Object,
            _outboxRepositoryMock.Object,
            new SubmitProposalValidator(),
            settings);

        _alwaysValidMock
            .Setup(v => v.ValidateAsync(It.IsAny<SubmitProposalInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _useCaseWithPassthroughValidator = new SubmitProposalUseCase(
            _repositoryMock.Object,
            _outboxRepositoryMock.Object,
            _alwaysValidMock.Object,
            settings);
    }

    private static SubmitProposalInput ValidInput() => new(
        Guid.NewGuid(),
        10_000m,
        12);

    private void SetupHappyPath()
    {
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldReturnGuid_ForValidInput()
    {
        SetupHappyPath();

        var result = await _useCase.ExecuteAsync(ValidInput());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldCreateOutboxMessage_ForValidInput()
    {
        SetupHappyPath();

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
        var result = await _useCase.ExecuteAsync(ValidInput() with { RequestedAmount = 50m });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WhenTermBelowMinimum()
    {
        var result = await _useCase.ExecuteAsync(ValidInput() with { TermMonths = 3 });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.Validation");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WhenCustomerIdIsEmpty()
    {
        var result = await _useCase.ExecuteAsync(ValidInput() with { CustomerId = Guid.Empty });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WithInvalidAmount_WhenAmountBelowMinimum_AfterValidation()
    {
        var input = ValidInput() with { RequestedAmount = 99m };

        var result = await _useCaseWithPassthroughValidator.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.InvalidAmount");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WithInvalidAmount_WhenAmountAboveMaximum_AfterValidation()
    {
        var input = ValidInput() with { RequestedAmount = 500_001m };

        var result = await _useCaseWithPassthroughValidator.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.InvalidAmount");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WithInvalidTerm_WhenTermBelowMinimum_AfterValidation()
    {
        var input = ValidInput() with { TermMonths = 5 };

        var result = await _useCaseWithPassthroughValidator.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.InvalidTerm");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldFail_WithInvalidTerm_WhenTermAboveMaximum_AfterValidation()
    {
        var input = ValidInput() with { TermMonths = 121 };

        var result = await _useCaseWithPassthroughValidator.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.InvalidTerm");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldUseDefaultRate_WhenMonthlyRateIsNull()
    {
        SetupHappyPath();
        Proposal? captured = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Callback<Proposal, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        await _useCase.ExecuteAsync(ValidInput()); // MonthlyRate = null

        captured!.MonthlyRate.Should().Be(1.8m);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_ShouldUseProvidedRate_WhenMonthlyRateIsNotNull()
    {
        SetupHappyPath();
        Proposal? captured = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Callback<Proposal, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        await _useCase.ExecuteAsync(ValidInput() with { MonthlyRate = 2.5m });

        captured!.MonthlyRate.Should().Be(2.5m);
    }
}
