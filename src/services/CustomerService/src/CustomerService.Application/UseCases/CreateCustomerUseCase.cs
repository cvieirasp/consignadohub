using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.Application.Ports;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Errors;
using CustomerService.Domain.ValueObjects;
using FluentValidation;

namespace CustomerService.Application.UseCases;

public sealed class CreateCustomerUseCase(
    ICustomerRepository repository,
    IValidator<CreateCustomerInput> validator)
{
    public async Task<Result<Guid>> ExecuteAsync(CreateCustomerInput input, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return CustomerErrors.ValidationFailed(string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));

        if (!Cpf.IsValid(input.Cpf))
            return CustomerErrors.InvalidCpf(input.Cpf);

        var cpf = Cpf.Create(input.Cpf);

        if (await repository.ExistsByCpfAsync(cpf.Value, ct))
            return CustomerErrors.CpfAlreadyExists(cpf.Value);

        var customer = Customer.Create(input.FullName, cpf, input.Email, input.Phone, input.BirthDate);
        await repository.AddAsync(customer, ct);
        await repository.SaveChangesAsync(ct);

        return customer.Id;
    }
}
