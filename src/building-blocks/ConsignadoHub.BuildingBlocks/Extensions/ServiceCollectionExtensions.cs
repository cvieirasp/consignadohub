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

    /// <summary>
    /// Configures JWT Bearer authentication using Keycloak settings from configuration.
    /// </summary>
    /// <param name="services">The service collection to add authentication services to.</param>
    /// <param name="configuration">The configuration containing Keycloak settings.</param>
    /// <returns>The updated service collection.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Load Keycloak settings from configuration.
        var settings = configuration
            .GetSection(KeycloakSettings.SectionName)
            .Get<KeycloakSettings>()
            ?? throw new InvalidOperationException("Keycloak configuration is missing.");

        // Configure JWT Bearer authentication with Keycloak settings.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                options.Audience = settings.Audience;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
            });

        services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();

        // Define authorization policies based on roles.
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(Policies.AnalystOrAdmin, policy =>
                policy.RequireRole(Roles.Admin, Roles.Analyst));
        });

        return services;
    }

    /// <summary>
    /// Configures OpenTelemetry tracing and metrics with sensible defaults for ConsignadoHub services.
    /// </summary>
    /// <param name="services">The service collection to add observability services to.</param>
    /// <param name="configuration">The configuration containing observability settings.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="serviceName">The name of the service for observability purposes.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddConsignadoHubObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName)
    {
        // Load observability settings from configuration, with defaults if not provided.
        var settings = configuration
            .GetSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>() ?? new ObservabilitySettings();

        // Configure OpenTelemetry with resource attributes, tracing, and metrics.
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

    /// <summary>
    /// Configures a RabbitMQ publisher using settings from configuration.
    /// </summary>
    /// <param name="services">The service collection to add the publisher to.</param>
    /// <param name="configuration">The configuration containing RabbitMQ settings.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register settings lazily so that configuration overrides applied after service
        // registration (e.g. WebApplicationFactory.ConfigureAppConfiguration in tests) are
        // picked up when the singleton is first resolved, not at DI wiring time.
        services.AddSingleton(sp =>
        {
            var cfg = sp.GetService<IConfiguration>() ?? configuration;
            return cfg.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
                ?? new RabbitMqSettings();
        });

        // Create and register the RabbitMQ publisher as a singleton, ensuring a shared connection.
        services.AddSingleton<IEventPublisher>(sp =>
        {
            var settings = sp.GetRequiredService<RabbitMqSettings>();
            return RabbitMqEventPublisher.CreateAsync(settings).GetAwaiter().GetResult();
        });

        // Expose the concrete publisher so consumers can access the shared connection.
        services.AddSingleton(sp =>
            (RabbitMqEventPublisher)sp.GetRequiredService<IEventPublisher>());

        return services;
    }
}
