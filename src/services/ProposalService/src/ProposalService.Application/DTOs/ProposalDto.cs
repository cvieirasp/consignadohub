using ProposalService.Domain.Enums;

namespace ProposalService.Application.DTOs;

public sealed record ProposalDto(
    Guid Id,
    Guid CustomerId,
    decimal RequestedAmount,
    int TermMonths,
    decimal MonthlyRate,
    decimal InstallmentAmount,
    decimal TotalAmount,
    decimal CET,
    ProposalStatus Status,
    DateTimeOffset CreatedAt);
