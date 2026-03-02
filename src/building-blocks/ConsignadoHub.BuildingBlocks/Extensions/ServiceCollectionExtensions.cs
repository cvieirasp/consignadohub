using ConsignadoHub.BuildingBlocks.Auth;
using ConsignadoHub.BuildingBlocks.Correlation;
using ConsignadoHub.BuildingBlocks.Messaging;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using ConsignadoHub.BuildingBlocks.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(KeycloakSettings.SectionName)
            .Get<KeycloakSettings>()
            ?? throw new InvalidOperationException("Keycloak configuration is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                options.Audience = settings.Audience;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
            });

        services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(Policies.AnalystOrAdmin, policy =>
                policy.RequireRole(Roles.Admin, Roles.Analyst));
        });

        return services;
    }

    public static IServiceCollection AddConsignadoHubObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName)
    {
        var settings = configuration
            .GetSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>() ?? new ObservabilitySettings();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(MessagingActivitySource.SourceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                if (environment.IsDevelopment())
                    tracing.AddConsoleExporter();

                if (!string.IsNullOrEmpty(settings.OtlpEndpoint))
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(settings.OtlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();

                if (environment.IsDevelopment())
                    metrics.AddConsoleExporter();

                if (!string.IsNullOrEmpty(settings.OtlpEndpoint))
                    metrics.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(settings.OtlpEndpoint));
            });

        return services;
    }

    public static IServiceCollection AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(RabbitMqSettings.SectionName)
            .Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddSingleton(settings);

        services.AddSingleton<IEventPublisher>(_ =>
            RabbitMqEventPublisher.CreateAsync(settings).GetAwaiter().GetResult());

        // Expose the concrete publisher so consumers can access the shared connection
        services.AddSingleton(sp =>
            (RabbitMqEventPublisher)sp.GetRequiredService<IEventPublisher>());

        return services;
    }
}
