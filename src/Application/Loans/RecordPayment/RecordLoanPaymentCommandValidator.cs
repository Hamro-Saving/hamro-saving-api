using FluentValidation;

namespace HamroSavings.Application.Loans.RecordPayment;

public sealed class RecordLoanPaymentCommandValidator : AbstractValidator<RecordLoanPaymentCommand>
{
    public RecordLoanPaymentCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty().WithMessage("Loan ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.PrincipalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Principal amount must be non-negative.");

        RuleFor(x => x.InterestAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Interest amount must be non-negative.");

        RuleFor(x => x.Amount)
            .Must((cmd, amount) => Math.Abs(amount - (cmd.PrincipalAmount + cmd.InterestAmount)) < 0.01m)
            .WithMessage("Amount must equal the sum of principal and interest amounts.");

        RuleFor(x => x.PaidDate)
            .NotEmpty().WithMessage("Paid date is required.");
    }
}
