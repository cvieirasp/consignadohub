namespace NotificationService.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit collection fixture that shares a single <see cref="NotificationServiceFactory"/>
/// (and therefore a single SQL Server + RabbitMQ container pair) across all notification
/// consumer test classes.
///
/// Without this, each test class with <c>IClassFixture&lt;NotificationServiceFactory&gt;</c>
/// would spin up its own container pair, quadrupling startup cost for a service where
/// all consumers follow the same idempotent fan-out pattern.
/// </summary>
[CollectionDefinition("NotificationService")]
public class NotificationServiceCollection : ICollectionFixture<NotificationServiceFactory> { }
