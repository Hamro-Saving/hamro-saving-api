using FluentValidation;

namespace HamroSavings.Application.Finance.CreateFixedDeposit;

public sealed class CreateFixedDepositCommandValidator : AbstractValidator<CreateFixedDepositCommand>
{
    public CreateFixedDepositCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.InstitutionName)
            .NotEmpty().WithMessage("Institution name is required.")
            .MaximumLength(200).WithMessage("Institution name must not exceed 200 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.InterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("Interest rate must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("Interest rate must not exceed 100.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.MaturityDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("Maturity date must be after start date.");
    }
}
