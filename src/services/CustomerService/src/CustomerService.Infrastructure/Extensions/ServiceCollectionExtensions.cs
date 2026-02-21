using CustomerService.Application.Ports;
using CustomerService.Infrastructure.Persistence;
using CustomerService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<CustomerDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CustomerDb"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }
}
