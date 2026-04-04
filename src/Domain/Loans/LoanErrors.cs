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

    public static readonly Error CannotModifyApproved =
        Error.Problem("Loan.CannotModifyApproved", "Cannot modify a loan that has already been approved.");

    public static readonly Error NotPending =
        Error.Problem("Loan.NotPending", "Loan is not in pending status.");

    public static readonly Error NotApproved =
        Error.Problem("Loan.NotApproved", "Loan must be approved before it can be verified.");

    public static readonly Error AlreadyApproved =
        Error.Conflict("Loan.AlreadyApproved", "You have already approved this loan.");

    public static readonly Error CannotSelfApprove =
        Error.Problem("Loan.CannotSelfApprove", "You cannot approve your own loan request.");

    public static readonly Error NotInGroup =
        Error.Forbidden("Loan.NotInGroup", "Loan does not belong to this group.");

    public static readonly Error ApprovalNotApplicable =
        Error.Problem("Loan.ApprovalNotApplicable", "Approval voting only applies to member loans.");
}
