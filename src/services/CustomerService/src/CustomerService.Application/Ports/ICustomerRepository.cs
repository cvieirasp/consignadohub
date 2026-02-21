using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Domain.Entities;

namespace CustomerService.Application.Ports;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByCpfAsync(string cpf, CancellationToken ct = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? name, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
