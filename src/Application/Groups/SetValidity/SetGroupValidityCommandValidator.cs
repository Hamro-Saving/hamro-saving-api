using FluentValidation;

namespace HamroSavings.Application.Groups.SetValidity;

public sealed class SetGroupValidityCommandValidator : AbstractValidator<SetGroupValidityCommand>
{
    public SetGroupValidityCommandValidator()
    {
        RuleFor(x => x.ValidFrom)
            .NotEmpty()
            .WithMessage("Valid From date is required.");

        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .When(x => x.ValidTo.HasValue)
            .WithMessage("Valid To must be after Valid From.");
    }
}
