using ProposalService.Domain.Enums;

namespace ProposalService.Application.DTOs;

public sealed record ListProposalsByCustomerInput(
    Guid CustomerId,
    ProposalStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
