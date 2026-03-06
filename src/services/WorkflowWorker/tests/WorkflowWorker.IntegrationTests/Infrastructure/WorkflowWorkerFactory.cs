using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using WorkflowWorker.Infrastructure.Persistence;

namespace WorkflowWorker.IntegrationTests.Infrastructure;

public sealed class WorkflowWorkerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private const string RabbitUser = "consignado";
    private const string RabbitPass = "consignado";

    private readonly RabbitMqContainer _rabbitContainer =
        new RabbitMqBuilder("rabbitmq:4-management")
            .WithUsername(RabbitUser)
            .WithPassword(RabbitPass)
            .Build();

    public string RabbitMqHostname => _rabbitContainer.Hostname;
    public int RabbitMqAmqpPort => _rabbitContainer.GetMappedPublicPort(5672);
    public string RabbitMqUsername => RabbitUser;
    public string RabbitMqPassword => RabbitPass;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Override connection strings before services are registered so the
        // DI-configured DbContext and RabbitMQ publisher target the test containers.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WorkflowDb"] = _sqlContainer.GetConnectionString(),
                ["RabbitMq:Host"]                = _rabbitContainer.Hostname,
                ["RabbitMq:Port"]                = _rabbitContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"]            = RabbitUser,
                ["RabbitMq:Password"]            = RabbitPass,
                ["RabbitMq:VirtualHost"]         = "/",
                ["RabbitMq:ExchangeName"]        = "consignadohub.events",
            });
        });
    }

    /// <summary>
    /// Creates a scoped WorkflowDbContext pointed at the test SQL Server container.
    /// </summary>
    public WorkflowDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    }

    /// <summary>
    /// Polls the InboxMessages table until the specified entry appears or the timeout elapses.
    /// A new scope (and thus a fresh DbContext) is created on every poll cycle to avoid
    /// reading stale first-level cache.
    /// </summary>
    public async Task WaitForInboxEntryAsync(
        Guid eventId,
        string consumerName,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));

        while (DateTime.UtcNow < deadline)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

            var exists = await db.InboxMessages
                .AsNoTracking()
                .AnyAsync(m => m.EventId == eventId && m.ConsumerName == consumerName);

            if (exists) return;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        Assert.Fail(
            $"Timeout waiting for InboxMessage. EventId={eventId}, ConsumerName={consumerName}");
    }

    public async Task InitializeAsync()
    {
        // Start both containers in parallel
        await Task.WhenAll(_sqlContainer.StartAsync(), _rabbitContainer.StartAsync());

        // Force the host to start. This resolves all singletons (including
        // RabbitMqEventPublisher) and starts the three BackgroundService consumers.
        _ = CreateClient();

        // Test environment skips the auto-migration in Program.cs, so we migrate here.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        await db.Database.MigrateAsync();

        // Wait until RabbitMQ is reachable and the consumers have declared their queues.
        await WaitForReadyAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    private async Task WaitForReadyAsync()
    {
        using var client = CreateClient();
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync("/health/ready");
                if (response.IsSuccessStatusCode) return;
            }
            catch
            {
                // not ready yet — swallow and retry
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask());
    }
}
