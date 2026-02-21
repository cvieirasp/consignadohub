using Asp.Versioning;
using ConsignadoHub.BuildingBlocks.Extensions;
using CustomerService.Api.Endpoints;
using CustomerService.Application.Extensions;
using CustomerService.Infrastructure.Extensions;
using CustomerService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddCustomerApplication();
builder.Services.AddCustomerInfrastructure(builder.Configuration);

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// OpenAPI / Scalar
builder.Services.AddOpenApi();

// ProblemDetails
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CustomerDbContext>("database");

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Apply migrations on startup in dev
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

// Versioned endpoints
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapGroup("/v1/customers")
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(1, 0)
    .WithTags("Customers")
    .MapCustomerEndpoints();

// Health checks
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
