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
