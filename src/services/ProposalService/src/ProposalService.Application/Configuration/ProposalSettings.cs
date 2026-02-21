namespace ProposalService.Application.Configuration;

public sealed class ProposalSettings
{
    public const string SectionName = "Proposal";

    /// <summary>Default monthly interest rate in percentage (e.g. 1.8 = 1.8% per month)</summary>
    public decimal DefaultMonthlyRate { get; init; } = 1.8m;
}
