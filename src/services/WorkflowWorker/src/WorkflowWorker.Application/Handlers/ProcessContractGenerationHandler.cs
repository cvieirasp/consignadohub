namespace WorkflowWorker.Application.Handlers;

public sealed class ProcessContractGenerationHandler
{
    public (Guid ContractId, string ContractUrl) Generate(Guid proposalId)
    {
        var contractId = Guid.NewGuid();
        var url = $"https://contracts.consignadohub.internal/{contractId:N}.pdf";
        return (contractId, url);
    }
}
