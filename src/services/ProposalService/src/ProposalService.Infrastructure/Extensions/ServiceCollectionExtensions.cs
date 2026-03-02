using ConsignadoHub.BuildingBlocks.Messaging.Inbox;
using ConsignadoHub.BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProposalService.Application.Ports;
using ProposalService.Infrastructure.Persistence;
using ProposalService.Infrastructure.Repositories;

namespace ProposalService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProposalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProposalDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ProposalDb"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IProposalRepository, ProposalRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();

        return services;
    }
}
