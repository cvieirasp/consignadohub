using CustomerService.Application.DTOs;
using FluentValidation;

namespace CustomerService.Application.Validators;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerInput>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today.AddYears(-18)))
            .WithMessage("Customer must be at least 18 years old.");
    }
}
