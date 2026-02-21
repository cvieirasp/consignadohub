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
    public void Execute_ShouldReturnSimulationResult_ForValidInput()
    {
        var input = new SimulateProposalInput(10_000m, 12);

        var result = _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequestedAmount.Should().Be(10_000m);
        result.Value.TermMonths.Should().Be(12);
        result.Value.MonthlyRate.Should().Be(1.8m);
        result.Value.InstallmentAmount.Should().BePositive();
        result.Value.TotalAmount.Should().BeGreaterThan(10_000m);
        result.Value.CET.Should().BePositive();
    }

    [Theory]
    [InlineData(99)]       // below minimum
    [InlineData(500_001)]  // above maximum
    public void Execute_ShouldFail_WhenAmountOutOfRange(decimal amount)
    {
        var input = new SimulateProposalInput(amount, 12);

        var result = _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProposalErrors.InvalidAmount.Code);
    }

    [Theory]
    [InlineData(5)]   // below minimum
    [InlineData(121)] // above maximum
    public void Execute_ShouldFail_WhenTermOutOfRange(int term)
    {
        var input = new SimulateProposalInput(10_000m, term);

        var result = _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProposalErrors.InvalidTerm.Code);
    }

    [Fact]
    public void Execute_ShouldUseProvidedRate_WhenSupplied()
    {
        var input = new SimulateProposalInput(10_000m, 12, MonthlyRate: 2.0m);

        var result = _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MonthlyRate.Should().Be(2.0m);
    }

    [Theory]
    [InlineData(100, 6)]        // boundary minimum
    [InlineData(500_000, 120)]  // boundary maximum
    public void Execute_ShouldSucceed_AtBoundaryValues(decimal amount, int term)
    {
        var result = _useCase.Execute(new SimulateProposalInput(amount, term));
        result.IsSuccess.Should().BeTrue();
    }
}
