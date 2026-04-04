using FluentValidation;

namespace HamroSavings.Application.Loans.UpdateLoan;

public sealed class UpdateLoanCommandValidator : AbstractValidator<UpdateLoanCommand>
{
    public UpdateLoanCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0).WithMessage("Interest rate cannot be negative.");
    }
}
