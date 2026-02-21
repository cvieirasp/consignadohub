using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.Application.Mappers;
using CustomerService.Application.Ports;
using CustomerService.Domain.Errors;

namespace CustomerService.Application.UseCases;

public sealed class GetCustomerByIdUseCase(ICustomerRepository repository)
{
    public async Task<Result<CustomerDto>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(id, ct);
        if (customer is null)
            return CustomerErrors.NotFound(id);

        return CustomerMapper.ToDto(customer);
    }
}
