using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public static class LoanErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Loan.NotFound", $"Loan with ID {id} was not found.");

    public static Error PaymentNotFound(Guid id) =>
        Error.NotFound("LoanPayment.NotFound", $"Loan payment with ID {id} was not found.");

    public static readonly Error NotActive =
        Error.Problem("Loan.NotActive", "Loan is not active.");

    public static readonly Error PaymentAlreadyVerified =
        Error.Conflict("LoanPayment.AlreadyVerified", "This payment has already been verified.");

    public static readonly Error NotInGroup =
        Error.Forbidden("Loan.NotInGroup", "Loan does not belong to this group.");
}
