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

        RuleFor(x => x.PrincipalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Principal amount must be non-negative.");

        RuleFor(x => x.InterestAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Interest amount must be non-negative.");

        RuleFor(x => x)
            .Must(cmd => cmd.PrincipalAmount + cmd.InterestAmount > 0)
            .WithMessage("A payment must put something towards principal or interest.");

        RuleFor(x => x.PaidDate)
            .NotEmpty().WithMessage("Paid date is required.");
    }
}
