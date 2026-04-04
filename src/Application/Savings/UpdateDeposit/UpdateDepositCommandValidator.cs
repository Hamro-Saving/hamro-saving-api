using FluentValidation;

namespace HamroSavings.Application.Savings.UpdateDeposit;

public sealed class UpdateDepositCommandValidator : AbstractValidator<UpdateDepositCommand>
{
    public UpdateDepositCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
