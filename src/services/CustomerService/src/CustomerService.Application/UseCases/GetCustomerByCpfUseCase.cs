using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.Application.Mappers;
using CustomerService.Application.Ports;
using CustomerService.Domain.Errors;
using CustomerService.Domain.ValueObjects;

namespace CustomerService.Application.UseCases;

public sealed class GetCustomerByCpfUseCase(ICustomerRepository repository)
{
    public async Task<Result<CustomerDto>> ExecuteAsync(string rawCpf, CancellationToken ct = default)
    {
        if (!Cpf.IsValid(rawCpf))
            return CustomerErrors.InvalidCpf(rawCpf);

        var cpf = Cpf.Create(rawCpf);
        var customer = await repository.GetByCpfAsync(cpf.Value, ct);
        if (customer is null)
            return CustomerErrors.NotFound(Guid.Empty);

        return CustomerMapper.ToDto(customer);
    }
}
