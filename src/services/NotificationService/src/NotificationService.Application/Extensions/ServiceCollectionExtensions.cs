using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Handlers;

namespace NotificationService.Application.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddScoped<NotificationHandler>();
        return services;
    }
}
