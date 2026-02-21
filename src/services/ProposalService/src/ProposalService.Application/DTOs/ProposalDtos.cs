using ProposalService.Domain.Enums;

namespace ProposalService.Application.DTOs;

public sealed record SimulationResultDto(
    decimal RequestedAmount,
    int TermMonths,
    decimal MonthlyRate,
    decimal InstallmentAmount,
    decimal TotalAmount,
    decimal CET);

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

public sealed record ProposalSummaryDto(
    Guid Id,
    Guid CustomerId,
    decimal RequestedAmount,
    int TermMonths,
    ProposalStatus Status,
    DateTimeOffset CreatedAt);

public sealed record ProposalTimelineEntryDto(
    Guid Id,
    ProposalStatus? FromStatus,
    ProposalStatus ToStatus,
    DateTimeOffset OccurredAt,
    string? Reason);

public sealed record SimulateProposalInput(
    decimal RequestedAmount,
    int TermMonths,
    decimal? MonthlyRate = null);

public sealed record SubmitProposalInput(
    Guid CustomerId,
    decimal RequestedAmount,
    int TermMonths,
    decimal? MonthlyRate = null);

public sealed record ListProposalsByCustomerInput(
    Guid CustomerId,
    ProposalStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
