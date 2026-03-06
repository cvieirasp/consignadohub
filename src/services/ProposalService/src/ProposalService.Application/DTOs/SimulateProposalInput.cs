namespace ProposalService.Application.DTOs;

public sealed record SimulateProposalInput(
    decimal RequestedAmount,
    int TermMonths,
    decimal? MonthlyRate = null);
