namespace CustomerService.Application.DTOs;

public sealed record CustomerDto(
    Guid Id,
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate,
    DateTimeOffset CreatedAt,
    bool IsActive);

public sealed record CustomerSummaryDto(
    Guid Id,
    string FullName,
    string Cpf,
    bool IsActive);

public sealed record CreateCustomerInput(
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate);

public sealed record UpdateCustomerInput(
    string FullName,
    string Email,
    string Phone);

public sealed record SearchCustomersInput(
    string? Name,
    int Page = 1,
    int PageSize = 20);
