namespace WorkflowWorker.Application.Handlers;

/// <summary>
/// Simulates the generation of a contract based on a proposal. 
/// In a real-world scenario, this would involve more complex logic, 
/// such as interacting with a document generation service, storing the 
/// contract in a database, and possibly sending notifications to relevant parties.
/// </summary>
public sealed class ProcessContractGenerationHandler
{
    public (Guid ContractId, string ContractUrl) Generate(Guid proposalId)
    {
        var contractId = Guid.NewGuid();
        var url = $"https://contracts.consignadohub.internal/{contractId:N}.pdf";
        return (contractId, url);
    }
}
