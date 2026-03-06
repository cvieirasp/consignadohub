namespace ProposalService.Application.DTOs;

public sealed record SubmitProposalInput(
    Guid CustomerId,
    decimal RequestedAmount,
    int TermMonths,
    decimal? MonthlyRate = null);
