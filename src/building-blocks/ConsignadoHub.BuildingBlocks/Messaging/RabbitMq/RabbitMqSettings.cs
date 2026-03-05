namespace ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Configuration settings for RabbitMQ connection and exchange details. 
/// It is a fallback to default values if not provided, ensuring that the 
/// application can run with minimal configuration for development purposes.
/// </summary>
public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string ExchangeName { get; init; } = "consignadohub.events";
}
