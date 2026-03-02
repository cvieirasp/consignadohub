namespace ConsignadoHub.BuildingBlocks.Observability;

public sealed class ObservabilitySettings
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// OTLP collector endpoint (e.g. "http://localhost:4317").
    /// When null/empty, only the console exporter is used.
    /// </summary>
    public string? OtlpEndpoint { get; init; }
}
