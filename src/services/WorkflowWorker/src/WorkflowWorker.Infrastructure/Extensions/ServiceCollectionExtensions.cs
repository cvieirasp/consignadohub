using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.Infrastructure.Persistence;
using WorkflowWorker.Infrastructure.Repositories;

namespace WorkflowWorker.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<WorkflowDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("WorkflowDb"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IInboxRepository, InboxRepository>();

        return services;
    }
}
