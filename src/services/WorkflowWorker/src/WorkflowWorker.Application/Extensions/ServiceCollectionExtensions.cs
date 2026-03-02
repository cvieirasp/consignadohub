using Microsoft.Extensions.DependencyInjection;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddScoped<ProcessCreditAnalysisHandler>();
        services.AddScoped<ProcessContractGenerationHandler>();
        services.AddScoped<ProcessDisbursementHandler>();
        return services;
    }
}
