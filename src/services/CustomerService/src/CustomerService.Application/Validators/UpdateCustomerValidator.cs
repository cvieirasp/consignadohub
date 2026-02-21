using CustomerService.Application.DTOs;
using FluentValidation;

namespace CustomerService.Application.Validators;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerInput>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
    }
}
