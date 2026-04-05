using FluentValidation;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Create;

public sealed class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(200).WithMessage("First name must not exceed 200 characters.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        When(x => x.MembershipType == MembershipType.Member, () =>
        {
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        });

        When(x => x.MembershipType == MembershipType.NonMember, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Email must be a valid email address.");
        });

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number must not exceed 30 characters.")
            .When(x => x.PhoneNumber is not null);
    }
}
