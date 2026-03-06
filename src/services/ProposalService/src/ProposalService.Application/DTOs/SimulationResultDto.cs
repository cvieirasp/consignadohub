namespace ProposalService.Application.DTOs;

public sealed record SimulationResultDto(
    decimal RequestedAmount,
    int TermMonths,
    decimal MonthlyRate,
    decimal InstallmentAmount,
    decimal TotalAmount,
    decimal CET);
