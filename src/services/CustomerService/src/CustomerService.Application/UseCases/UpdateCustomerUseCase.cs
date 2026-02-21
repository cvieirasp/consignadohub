using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.Application.Ports;
using CustomerService.Domain.Errors;
using FluentValidation;

namespace CustomerService.Application.UseCases;

public sealed class UpdateCustomerUseCase(
    ICustomerRepository repository,
    IValidator<UpdateCustomerInput> validator)
{
    public async Task<Result> ExecuteAsync(Guid id, UpdateCustomerInput input, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return CustomerErrors.ValidationFailed(string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));

        var customer = await repository.GetByIdTrackedAsync(id, ct);
        if (customer is null)
            return CustomerErrors.NotFound(id);

        if (!customer.IsActive)
            return CustomerErrors.CustomerInactive;

        customer.Update(input.FullName, input.Email, input.Phone);
        await repository.SaveChangesAsync(ct);

        return Result.Success();
    }
}
