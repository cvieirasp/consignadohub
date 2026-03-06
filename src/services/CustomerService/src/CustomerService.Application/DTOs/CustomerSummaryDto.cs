namespace CustomerService.Application.DTOs;

public sealed record CustomerSummaryDto(
    Guid Id,
    string FullName,
    string Cpf,
    bool IsActive);
