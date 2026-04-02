using FluentValidation;

namespace HamroSavings.Application.Groups.Create;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(200).WithMessage("Group name must not exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Group code is required.")
            .MaximumLength(20).WithMessage("Group code must not exceed 20 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Group code must contain only alphanumeric characters.");

        RuleFor(x => x.MemberInterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("Member interest rate must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("Member interest rate must not exceed 100.");

        RuleFor(x => x.NonMemberInterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("Non-member interest rate must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("Non-member interest rate must not exceed 100.");
    }
}
