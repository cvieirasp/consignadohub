using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.Ports;
using CustomerService.Domain.Errors;

namespace CustomerService.Application.UseCases;

public sealed class DeactivateCustomerUseCase(ICustomerRepository repository)
{
    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdTrackedAsync(id, ct);
        if (customer is null)
            return CustomerErrors.NotFound(id);

        if (!customer.IsActive)
            return CustomerErrors.CustomerInactive;

        customer.Deactivate();
        await repository.SaveChangesAsync(ct);

        return Result.Success();
    }
}
