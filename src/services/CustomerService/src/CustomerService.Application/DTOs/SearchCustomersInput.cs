namespace CustomerService.Application.DTOs;

public sealed record SearchCustomersInput(
    string? Name,
    int Page = 1,
    int PageSize = 20);
