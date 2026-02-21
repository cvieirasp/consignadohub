using FluentAssertions;
using ProposalService.Domain.Entities;

namespace ProposalService.UnitTests.Domain;

public sealed class ProposalCalculationTests
{
    [Theory]
    [InlineData(10000, 12, 1.8, 934.02)]   // PMT for 10k @ 1.8%/month for 12 months
    [InlineData(50000, 60, 1.8, 1369.60)]  // 50k @ 1.8%/month for 60 months
    public void CalculateFinancials_ShouldReturnCorrectInstallment(
        decimal amount, int term, decimal rate, decimal expectedInstallment)
    {
        var (installment, _, _) = Proposal.CalculateFinancials(amount, term, rate);

        installment.Should().BeApproximately(expectedInstallment, 0.5m);
    }

    [Fact]
    public void CalculateFinancials_TotalAmount_ShouldBeInstallmentTimesTermMonths()
    {
        var (installment, total, _) = Proposal.CalculateFinancials(10_000m, 12, 1.8m);

        var expectedTotal = Math.Round(installment * 12, 2);
        total.Should().BeApproximately(expectedTotal, 0.01m);
    }

    [Fact]
    public void CalculateFinancials_CET_ShouldBePositive()
    {
        var (_, _, cet) = Proposal.CalculateFinancials(10_000m, 12, 1.8m);
        cet.Should().BePositive();
    }

    [Fact]
    public void CalculateFinancials_ZeroRate_ShouldReturnAmountDividedByTerm()
    {
        var (installment, total, _) = Proposal.CalculateFinancials(12_000m, 12, 0m);

        installment.Should().BeApproximately(1_000m, 0.01m);
        total.Should().BeApproximately(12_000m, 0.01m);
    }

    [Fact]
    public void Create_ShouldPopulateAllFields()
    {
        var customerId = Guid.NewGuid();
        var proposal = Proposal.Create(customerId, 10_000m, 12, 1.8m);

        proposal.Id.Should().NotBeEmpty();
        proposal.CustomerId.Should().Be(customerId);
        proposal.RequestedAmount.Should().Be(10_000m);
        proposal.TermMonths.Should().Be(12);
        proposal.MonthlyRate.Should().Be(1.8m);
        proposal.InstallmentAmount.Should().BePositive();
        proposal.TotalAmount.Should().BeGreaterThan(10_000m);
        proposal.CET.Should().BePositive();
    }
}
