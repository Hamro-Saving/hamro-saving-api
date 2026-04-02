using FluentValidation;

namespace HamroSavings.Application.Groups.Update;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(200).WithMessage("Group name must not exceed 200 characters.");

        RuleFor(x => x.MemberInterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("Member interest rate must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("Member interest rate must not exceed 100.");

        RuleFor(x => x.NonMemberInterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("Non-member interest rate must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("Non-member interest rate must not exceed 100.");
    }
}
