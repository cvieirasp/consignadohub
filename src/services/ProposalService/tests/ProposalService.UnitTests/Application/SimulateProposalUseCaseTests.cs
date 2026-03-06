using FluentAssertions;
using Microsoft.Extensions.Options;
using ProposalService.Application.Configuration;
using ProposalService.Application.DTOs;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Errors;

namespace ProposalService.UnitTests.Application;

public sealed class SimulateProposalUseCaseTests
{
    private readonly SimulateProposalUseCase _useCase;

    public SimulateProposalUseCaseTests()
    {
        var settings = Options.Create(new ProposalSettings { DefaultMonthlyRate = 1.8m });
        _useCase = new SimulateProposalUseCase(settings);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldReturnSimulationResult_UsingDefaultRate_WhenMonthlyRateIsNull()
    {
        // MonthlyRate = null - uses DefaultMonthlyRate (1.8m)
        var input = new SimulateProposalInput(10_000m, 12);

        var result = _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MonthlyRate.Should().Be(1.8m);
        result.Value.InstallmentAmount.Should().BePositive();
        result.Value.TotalAmount.Should().BeGreaterThan(10_000m);
        result.Value.CET.Should().BePositive();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldReturnSimulationResult_UsingProvidedRate_WhenMonthlyRateIsNotNull()
    {
        var input = new SimulateProposalInput(10_000m, 12, MonthlyRate: 2.5m);

        var result = _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MonthlyRate.Should().Be(2.5m);
        result.Value.TotalAmount.Should().BeGreaterThan(10_000m);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldReturnSimulationResult_WithZeroInterest_WhenMonthlyRateIsZero()
    {
        var input = new SimulateProposalInput(10_000m, 12, MonthlyRate: 0m);

        var result = _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MonthlyRate.Should().Be(0m);
        // With zero interest, InstallmentAmount should be TotalAmount / TermMonths, 
        // and TotalAmount should equal RequestedAmount
        result.Value.InstallmentAmount.Should().Be(833.33m);
        // TotalAmount should be exactly 10_000m, but due to rounding in the calculation, 
        // it may be slightly off.
        result.Value.TotalAmount.Should().Be(10_000m);
        result.Value.CET.Should().Be(0m);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldFail_WhenAmountBelowMinimum()
    {
        // Amount = 99m - below minimum
        var result = _useCase.Execute(new SimulateProposalInput(99m, 12));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProposalErrors.InvalidAmount.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldFail_WhenAmountAboveMaximum()
    {
        // Amount = 500_001m - above maximum
        var result = _useCase.Execute(new SimulateProposalInput(500_001m, 12));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProposalErrors.InvalidAmount.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldFail_WhenTermBelowMinimum()
    {
        // Term = 5 - below minimum
        var result = _useCase.Execute(new SimulateProposalInput(10_000m, 5));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProposalErrors.InvalidTerm.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_ShouldFail_WhenTermAboveMaximum()
    {
        // Term = 121 - above maximum
        var result = _useCase.Execute(new SimulateProposalInput(10_000m, 121));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProposalErrors.InvalidTerm.Code);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(100, 6)]        // boundary minimum
    [InlineData(500_000, 120)]  // boundary maximum
    public void Execute_ShouldSucceed_AtBoundaryValues(decimal amount, int term)
    {
        var result = _useCase.Execute(new SimulateProposalInput(amount, term));
        result.IsSuccess.Should().BeTrue();
    }
}
