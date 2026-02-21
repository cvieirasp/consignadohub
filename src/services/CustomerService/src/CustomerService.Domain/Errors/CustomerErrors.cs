using ConsignadoHub.BuildingBlocks.Results;

namespace CustomerService.Domain.Errors;

public static class CustomerErrors
{
    public static Error NotFound(Guid id) =>
        new("Customer.NotFound", $"Customer '{id}' was not found.");

    public static Error CpfAlreadyExists(string cpf) =>
        new("Customer.AlreadyExists", $"A customer with CPF '{cpf}' already exists.");

    public static Error InvalidCpf(string cpf) =>
        new("Customer.InvalidCpf", $"'{cpf}' is not a valid CPF.");

    public static readonly Error CustomerInactive =
        new("Customer.Inactive", "The customer account is inactive.");

    public static Error ValidationFailed(string message = "One or more validation errors occurred.") =>
        new("Customer.Validation", message);
}
