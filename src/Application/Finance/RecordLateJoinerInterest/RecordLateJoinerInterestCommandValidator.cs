using FluentValidation;

namespace HamroSavings.Application.Finance.RecordLateJoinerInterest;

public sealed class RecordLateJoinerInterestCommandValidator : AbstractValidator<RecordLateJoinerInterestCommand>
{
    public RecordLateJoinerInterestCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty().WithMessage("Choose the member who paid.");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.PaidDate)
            .NotEmpty().WithMessage("Paid date is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("Paid date cannot be in the future.");

        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
