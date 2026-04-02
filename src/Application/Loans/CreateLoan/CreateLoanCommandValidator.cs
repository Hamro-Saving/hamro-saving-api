using FluentValidation;

namespace HamroSavings.Application.Loans.CreateLoan;

public sealed class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        RuleFor(x => x.BorrowerId)
            .NotEmpty().WithMessage("Borrower ID is required.");

        RuleFor(x => x.BorrowerType)
            .NotEmpty().WithMessage("Borrower type is required.")
            .Must(t => t == "Member" || t == "NonMember")
            .WithMessage("Borrower type must be 'Member' or 'NonMember'.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Loan amount must be greater than zero.");

        RuleFor(x => x.InterestRate)
            .InclusiveBetween(0, 100)
            .When(x => x.InterestRate.HasValue)
            .WithMessage("Interest rate must be between 0 and 100.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.DueDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be after start date.");
    }
}
