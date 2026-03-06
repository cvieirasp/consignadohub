namespace WorkflowWorker.Application.Handlers;

/// <summary>
/// Simulates a credit analysis process for a loan application. 
/// It generates a random credit score and determines approval based 
/// on a predefined threshold. The reason for approval or denial is also 
/// provided for transparency in the decision-making process.
/// </summary>
public sealed class ProcessCreditAnalysisHandler
{
    public (bool Approved, int Score, string Reason) Analyze(
        Guid proposalId,
        decimal requestedAmount,
        int termMonths)
    {
        var score = Random.Shared.Next(300, 901);
        var approved = score >= 600;

        var reason = approved
            ? $"Credit approved. Score: {score}. Application meets all lending criteria."
            : $"Credit denied. Score: {score}. Score below minimum threshold of 600.";

        return (approved, score, reason);
    }
}
