using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<NotificationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("NotificationDb"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IInboxRepository, InboxRepository>();

        return services;
    }
}
