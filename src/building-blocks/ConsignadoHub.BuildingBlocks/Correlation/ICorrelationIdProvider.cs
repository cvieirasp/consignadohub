namespace ConsignadoHub.BuildingBlocks.Correlation;

public interface ICorrelationIdProvider
{
    string CorrelationId { get; }
}
