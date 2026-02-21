using FluentValidation;
using ProposalService.Application.DTOs;

namespace ProposalService.Application.Validators;

public sealed class SubmitProposalValidator : AbstractValidator<SubmitProposalInput>
{
    public SubmitProposalValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.RequestedAmount)
            .GreaterThanOrEqualTo(100m)
            .LessThanOrEqualTo(500_000m);
        RuleFor(x => x.TermMonths)
            .GreaterThanOrEqualTo(6)
            .LessThanOrEqualTo(120);
        RuleFor(x => x.MonthlyRate)
            .GreaterThan(0).When(x => x.MonthlyRate.HasValue);
    }
}
