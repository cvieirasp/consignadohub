using ConsignadoHub.BuildingBlocks.Extensions;
using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Extensions;
using NotificationService.Infrastructure.Extensions;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Worker.Consumers;
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
builder.Services.AddConsignadoHubObservability(builder.Configuration, builder.Environment, "NotificationService");
builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(builder.Configuration);

// Messaging
builder.Services.AddRabbitMqPublisher(builder.Configuration);
builder.Services.AddHostedService<ProposalSubmittedNotificationConsumer>();
builder.Services.AddHostedService<CreditAnalysisCompletedNotificationConsumer>();
builder.Services.AddHostedService<ContractGeneratedNotificationConsumer>();
builder.Services.AddHostedService<DisbursementCompletedNotificationConsumer>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>("database")
    .AddRabbitMQ(
        sp => sp.GetRequiredService<RabbitMqEventPublisher>().GetConnection(),
        name: "rabbitmq");

var app = builder.Build();

app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    // Apply migrations on startup in dev
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

// Health checks
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
