using ConsignadoHub.BuildingBlocks.Results;
using ProposalService.Domain.Errors;
using ProposalService.Domain.Enums;

namespace ProposalService.Domain.Entities;

public sealed class Proposal
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public int TermMonths { get; private set; }
    public decimal MonthlyRate { get; private set; }
    public ProposalStatus Status { get; private set; }
    public decimal InstallmentAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal CET { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<ProposalTimelineEntry> _timeline = [];
    public IReadOnlyList<ProposalTimelineEntry> Timeline => _timeline.AsReadOnly();

    private Proposal() { }

    public static Proposal Create(Guid customerId, decimal requestedAmount, int termMonths, decimal monthlyRate)
    {
        var (installment, total, cet) = CalculateFinancials(requestedAmount, termMonths, monthlyRate);

        return new Proposal
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            RequestedAmount = requestedAmount,
            TermMonths = termMonths,
            MonthlyRate = monthlyRate,
            Status = ProposalStatus.Submitted,
            InstallmentAmount = installment,
            TotalAmount = total,
            CET = cet,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void AddTimelineEntry(ProposalStatus? fromStatus, ProposalStatus toStatus, string? reason = null)
    {
        _timeline.Add(new ProposalTimelineEntry(Guid.NewGuid(), Id, fromStatus, toStatus, DateTimeOffset.UtcNow, reason));
    }

    private static readonly HashSet<ProposalStatus> TerminalStatuses =
    [
        ProposalStatus.Rejected,
        ProposalStatus.Disbursed,
    ];

    public Result UpdateStatus(ProposalStatus newStatus, string? reason = null)
    {
        if (TerminalStatuses.Contains(Status))
            return ProposalErrors.InvalidStatusTransition(Status, newStatus);

        var previous = Status;
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddTimelineEntry(previous, newStatus, reason);

        return Result.Success();
    }

    /// <summary>
    /// Calculates the installment, total amount to be paid, and CET based on the requested amount, 
    /// term in months, and monthly interest rate (PV * r / (1 - (1+r)^-n)).
    /// </summary>
    /// <param name="requestedAmount">The amount requested by the customer.</param>
    /// <param name="termMonths">The term of the loan in months.</param>
    /// <param name="monthlyRate">The monthly interest rate.</param>
    /// <returns>A tuple containing the installment amount, total amount to be paid, and CET.</returns>
    public static (decimal Installment, decimal Total, decimal Cet) CalculateFinancials(
        decimal requestedAmount, int termMonths, decimal monthlyRate)
    {
        var r = (double)monthlyRate / 100.0;
        var n = termMonths;

        double installment;
        if (r == 0)
        {
            installment = (double)requestedAmount / n;
        }
        else
        {
            installment = (double)requestedAmount * r / (1 - Math.Pow(1 + r, -n));
        }

        var total = installment * n;
        var cet = (total - (double)requestedAmount) / (double)requestedAmount;

        return (
            Math.Round((decimal)installment, 2),
            Math.Round((decimal)total, 2),
            Math.Round((decimal)cet, 6));
    }
}
