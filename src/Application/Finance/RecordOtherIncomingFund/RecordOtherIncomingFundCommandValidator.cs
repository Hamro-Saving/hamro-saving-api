using FluentValidation;

namespace HamroSavings.Application.Finance.RecordOtherIncomingFund;

public sealed class RecordOtherIncomingFundCommandValidator : AbstractValidator<RecordOtherIncomingFundCommand>
{
    public RecordOtherIncomingFundCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty().WithMessage("Choose the member who paid.");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.PaidDate)
            .NotEmpty().WithMessage("Paid date is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("Paid date cannot be in the future.");

        RuleFor(x => x.Remarks)
            .NotEmpty().WithMessage("Remarks are required — say what this money was for.")
            .MaximumLength(500);
    }
}
