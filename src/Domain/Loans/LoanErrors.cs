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

    public static readonly Error PaymentBeforeLastTransaction =
        Error.Problem("Loan.PaymentBeforeLastTransaction", "A payment cannot be dated before the loan's last settled transaction.");

    public static readonly Error PaymentInFuture =
        Error.Problem("Loan.PaymentInFuture", "A payment cannot be dated in the future.");

    public static readonly Error PrincipalExceedsOutstanding =
        Error.Problem("Loan.PrincipalExceedsOutstanding", "The principal paid is more than the principal still outstanding.");

    public static readonly Error PaymentAlreadyVerified =
        Error.Conflict("LoanPayment.AlreadyVerified", "This payment has already been verified.");

    public static readonly Error CannotModifyVerifiedPayment =
        Error.Conflict("LoanPayment.CannotModifyVerified", "This payment has been verified and is in the group's books, so it can no longer be changed or removed.");

    /// <summary>
    /// Replaying the loan would move a payment that is already posted, so the correction is
    /// refused rather than left to disagree with the books.
    /// </summary>
    public static readonly Error VerifiedPaymentAfter =
        Error.Conflict("LoanPayment.VerifiedPaymentAfter", "A later payment on this loan has already been verified, so this one can no longer be changed.");

    public static readonly Error NotDisbursed =
        Error.Problem("Loan.NotDisbursed", "This loan has not been disbursed, so it has no payments to settle.");

    public static readonly Error CannotDeleteUnlessCancelled =
        Error.Conflict("Loan.CannotDeleteUnlessCancelled", "Only a cancelled loan can be deleted.");

    public static readonly Error InLedger =
        Error.Conflict("Loan.InLedger", "This record has entries in the group's books and cannot be deleted.");

    public static readonly Error CannotModifyAfterDisbursement =
        Error.Problem("Loan.CannotModifyAfterDisbursement", "A loan can only be changed before the money leaves the group.");

    public static readonly Error CannotModifyApproved =
        Error.Problem("Loan.CannotModifyApproved", "Cannot modify a loan that has already been approved.");

    public static readonly Error NotPending =
        Error.Problem("Loan.NotPending", "Loan is not in pending status.");

    public static readonly Error NotApproved =
        Error.Problem("Loan.NotApproved", "Loan must be approved by the members before it can be disbursed.");

    public static readonly Error DisbursedAmountNotPositive =
        Error.Validation("Loan.DisbursedAmountNotPositive", "The disbursed amount must be greater than zero.");

    /// <summary>The group can hand over less than was asked for, but never more.</summary>
    public static Error DisbursedAmountExceedsRequest(decimal disbursed, decimal requested) =>
        Error.Validation(
            "Loan.DisbursedAmountExceedsRequest",
            $"Cannot disburse {disbursed:N0}: this loan was only approved for {requested:N0}.");

    public static readonly Error DisbursementInFuture =
        Error.Validation("Loan.DisbursementInFuture", "A loan cannot be disbursed on a future date.");

    public static readonly Error GroupRefusedLoan =
        Error.Conflict("Loan.GroupRefusedLoan", "The members have declined this loan, so it cannot be force disbursed.");

    public static readonly Error CannotForceDisburse =
        Error.Conflict("Loan.CannotForceDisburse", "Only a loan still awaiting disbursement can be force disbursed.");

    public static readonly Error CannotCancelAfterDisbursement =
        Error.Problem("Loan.CannotCancelAfterDisbursement", "A loan can only be cancelled before its disbursement starts.");

    public static readonly Error AlreadyVoted =
        Error.Conflict("Loan.AlreadyVoted", "You have already voted on this loan.");

    public static readonly Error NotEligibleToVote =
        Error.Forbidden("Loan.NotEligibleToVote", "Only active group members can approve or decline loans.");

    public static readonly Error CannotSelfVote =
        Error.Problem("Loan.CannotSelfVote", "You cannot vote on your own loan request.");

    public static readonly Error NotInGroup =
        Error.Forbidden("Loan.NotInGroup", "Loan does not belong to this group.");
}
