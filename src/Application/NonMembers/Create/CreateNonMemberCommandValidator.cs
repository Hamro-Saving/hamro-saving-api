using FluentValidation;

namespace HamroSavings.Application.NonMembers.Create;

public sealed class CreateNonMemberCommandValidator : AbstractValidator<CreateNonMemberCommand>
{
    public CreateNonMemberCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");
    }
}
