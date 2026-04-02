using FluentValidation;

namespace HamroSavings.Application.Members.AssignAdmin;

public sealed class AssignAdminCommandValidator : AbstractValidator<AssignAdminCommand>
{
    public AssignAdminCommandValidator()
    {
        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");
    }
}
