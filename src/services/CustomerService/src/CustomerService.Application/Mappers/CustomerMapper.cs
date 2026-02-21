using CustomerService.Application.DTOs;
using CustomerService.Domain.Entities;

namespace CustomerService.Application.Mappers;

internal static class CustomerMapper
{
    public static CustomerDto ToDto(Customer c) => new(
        c.Id,
        c.FullName,
        c.Cpf.Value,
        c.Email,
        c.Phone,
        c.BirthDate,
        c.CreatedAt,
        c.IsActive);

    public static CustomerSummaryDto ToSummaryDto(Customer c) => new(
        c.Id,
        c.FullName,
        c.Cpf.Value,
        c.IsActive);
}
