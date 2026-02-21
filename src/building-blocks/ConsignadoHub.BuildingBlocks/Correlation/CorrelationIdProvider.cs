namespace ConsignadoHub.BuildingBlocks.Correlation;

internal sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    public string CorrelationId { get; set; } = string.Empty;
}
