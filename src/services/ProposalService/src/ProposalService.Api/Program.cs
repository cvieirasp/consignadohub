using Asp.Versioning;
using ConsignadoHub.BuildingBlocks.Extensions;
using ConsignadoHub.BuildingBlocks.Messaging.Outbox;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProposalService.Api.Consumers;
using ProposalService.Api.Endpoints;
using ProposalService.Application.Configuration;
using ProposalService.Application.Extensions;
using ProposalService.Infrastructure.Extensions;
using ProposalService.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

// Services
builder.Services.AddBuildingBlocks();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddConsignadoHubObservability(builder.Configuration, builder.Environment, "ProposalService");
builder.Services.AddProposalApplication(settings =>
    builder.Configuration.GetSection(ProposalSettings.SectionName).Bind(settings));
builder.Services.AddProposalInfrastructure(builder.Configuration);

// Messaging
builder.Services.AddRabbitMqPublisher(builder.Configuration);
builder.Services.AddHostedService<OutboxDispatcherHostedService>();
builder.Services.AddHostedService<CreditAnalysisCompletedConsumer>();
builder.Services.AddHostedService<ContractGeneratedConsumer>();
builder.Services.AddHostedService<DisbursementCompletedConsumer>();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// OpenAPI / Scalar
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["BearerAuth"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT token from Keycloak. Acquire via: POST /realms/consignadohub/protocol/openid-connect/token"
        };
        return Task.CompletedTask;
    });
});

// ProblemDetails
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProposalDbContext>("database")
    .AddRabbitMQ(
        sp => sp.GetRequiredService<RabbitMqEventPublisher>().GetConnection(),
        name: "rabbitmq");

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("ConsignadoHub — ProposalService API")
            .WithPreferredScheme("BearerAuth")
            .WithHttpBearerAuthentication(bearer => bearer.Token = string.Empty);
    });

    // Apply migrations on startup in dev
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProposalDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

// Versioned endpoints
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapGroup("/v1/proposals")
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(1, 0)
    .WithTags("Proposals")
    .RequireAuthorization()
    .MapProposalEndpoints();

// Health checks
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
