using FluentValidation;

namespace HamroSavings.Application.Auth.Signup;

public sealed class SignupWithTokenCommandValidator : AbstractValidator<SignupWithTokenCommand>
{
    public SignupWithTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Signup token is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
