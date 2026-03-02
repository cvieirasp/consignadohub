using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace NotificationService.IntegrationTests.Infrastructure;

public sealed class NotificationServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly RabbitMqContainer _rabbitContainer =
        new RabbitMqBuilder("rabbitmq:4-management").Build();

    public string RabbitMqHostname => _rabbitContainer.Hostname;
    public int RabbitMqAmqpPort => _rabbitContainer.GetMappedPublicPort(5672);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Override connection strings before DI registers services so the
        // DbContext and RabbitMQ publisher point at the test containers.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotificationDb"] = _sqlContainer.GetConnectionString(),
                ["RabbitMq:Host"]                    = _rabbitContainer.Hostname,
                ["RabbitMq:Port"]                    = _rabbitContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"]                = "guest",
                ["RabbitMq:Password"]                = "guest",
                ["RabbitMq:VirtualHost"]             = "/",
                ["RabbitMq:ExchangeName"]            = "consignadohub.events",
            });
        });
    }

    /// <summary>
    /// Polls InboxMessages until the given entry appears or the timeout elapses.
    /// A fresh EF scope is created on each cycle to bypass the first-level cache.
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
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

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

        // Force the host to start — resolves all singletons and starts the
        // four BackgroundService consumers.
        _ = CreateClient();

        // Test environment skips the auto-migration in Program.cs, so we migrate here.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await db.Database.MigrateAsync();

        // Wait for RabbitMQ readiness then give consumers time to declare their queues.
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
