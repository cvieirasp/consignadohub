using ProposalService.Domain.Enums;

namespace ProposalService.Domain.Entities;

public sealed class ProposalTimelineEntry
{
    public Guid Id { get; private set; }
    public Guid ProposalId { get; private set; }
    public ProposalStatus? FromStatus { get; private set; }
    public ProposalStatus ToStatus { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Reason { get; private set; }

    internal ProposalTimelineEntry(
        Guid id,
        Guid proposalId,
        ProposalStatus? fromStatus,
        ProposalStatus toStatus,
        DateTimeOffset occurredAt,
        string? reason)
    {
        Id = id;
        ProposalId = proposalId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        OccurredAt = occurredAt;
        Reason = reason;
    }
}
