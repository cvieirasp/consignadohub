namespace CustomerService.Application.DTOs;

public sealed record CreateCustomerInput(
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate);
