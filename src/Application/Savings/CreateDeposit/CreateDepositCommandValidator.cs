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
            .GreaterThan(2000).WithMessage("Deposit year must be greater than 2000.")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1).WithMessage("Deposit year cannot be in the far future.");
    }
}
