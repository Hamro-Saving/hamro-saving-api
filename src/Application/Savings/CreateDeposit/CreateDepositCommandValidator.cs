using FluentValidation;

namespace HamroSavings.Application.Savings.CreateDeposit;

public sealed class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.DepositMonth)
            .InclusiveBetween(1, 12).WithMessage("Deposit month must be between 1 and 12.");

        RuleFor(x => x.DepositYear)
            .GreaterThan(2070).WithMessage("Deposit year must be greater than 2070 (BS).")
            .LessThanOrEqualTo(2100).WithMessage("Deposit year cannot be in the far future.");

        RuleFor(x => x.DepositDate)
            .NotEmpty().WithMessage("Deposit date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Deposit date cannot be in the future.");
    }
}
