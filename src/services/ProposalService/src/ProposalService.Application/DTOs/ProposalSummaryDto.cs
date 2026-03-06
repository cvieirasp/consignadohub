using ProposalService.Domain.Enums;

namespace ProposalService.Application.DTOs;

public sealed record ProposalSummaryDto(
    Guid Id,
    Guid CustomerId,
    decimal RequestedAmount,
    int TermMonths,
    ProposalStatus Status,
    DateTimeOffset CreatedAt);
