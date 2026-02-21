using ConsignadoHub.BuildingBlocks.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ConsignadoHub.BuildingBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
