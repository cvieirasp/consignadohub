using ProposalService.Domain.Enums;

namespace ProposalService.Application.DTOs;

public sealed record ProposalTimelineEntryDto(
    Guid Id,
    ProposalStatus? FromStatus,
    ProposalStatus ToStatus,
    DateTimeOffset OccurredAt,
    string? Reason);
