using CustomerService.Application.Ports;
using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Repositories;

internal sealed class CustomerRepository(CustomerDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default) =>
        await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer?> GetByCpfAsync(string cpf, CancellationToken ct = default)
    {
        var cpfValue = Cpf.Create(cpf);
        return await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Cpf == cpfValue, ct);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default)
    {
        var cpfValue = Cpf.Create(cpf);
        return await db.Customers.AnyAsync(c => c.Cpf == cpfValue, ct);
    }

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? name, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.FullName.Contains(name));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default) =>
        await db.Customers.AddAsync(customer, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
