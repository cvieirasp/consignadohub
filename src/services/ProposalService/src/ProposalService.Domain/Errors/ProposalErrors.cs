using ConsignadoHub.BuildingBlocks.Results;

namespace ProposalService.Domain.Errors;

public static class ProposalErrors
{
    public static Error NotFound(Guid id) =>
        new("Proposal.NotFound", $"Proposal '{id}' was not found.");

    public static readonly Error InvalidAmount =
        new("Proposal.InvalidAmount", "Requested amount must be between 100 and 500,000.");

    public static readonly Error InvalidTerm =
        new("Proposal.InvalidTerm", "Term must be between 6 and 120 months.");

    public static Error ValidationFailed(string message = "One or more validation errors occurred.") =>
        new("Proposal.Validation", message);
}
