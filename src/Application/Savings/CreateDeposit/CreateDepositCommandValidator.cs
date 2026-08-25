using FluentValidation;
using HamroSavings.Domain.Savings;

namespace HamroSavings.Application.Savings.CreateDeposit;

public sealed class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => t.CanBeRecorded())
            .WithMessage("Loan interest and repayments are recorded against the loan, not as a deposit.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        When(x => x.Type == DepositType.MonthlyDeposit, () =>
        {
            RuleFor(x => x.DepositMonth)
                .NotNull().WithMessage("A monthly deposit needs the month it covers.")
                .InclusiveBetween(1, 12).WithMessage("Deposit month must be between 1 and 12.");

            RuleFor(x => x.DepositYear)
                .NotNull().WithMessage("A monthly deposit needs the year it covers.")
                .GreaterThan(2070).WithMessage("Deposit year must be greater than 2070 (BS).")
                .LessThanOrEqualTo(2100).WithMessage("Deposit year cannot be in the far future.");
        });

        RuleFor(x => x.DepositDate)
            .NotEmpty().WithMessage("Deposit date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Deposit date cannot be in the future.");
    }
}
