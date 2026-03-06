namespace CustomerService.Application.DTOs;

public sealed record UpdateCustomerInput(
    string FullName,
    string Email,
    string Phone);
