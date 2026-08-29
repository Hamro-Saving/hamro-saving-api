using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

public static class ExpenseErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Expense.NotFound", $"Expense with ID {id} was not found.");

    public static readonly Error NotInGroup =
        Error.Forbidden("Expense.NotInGroup", "Expense does not belong to this group.");

    public static readonly Error AlreadyVerified =
        Error.Conflict("Expense.AlreadyVerified", "This expense has already been verified.");

    public static readonly Error CannotModifyVerified =
        Error.Conflict("Expense.CannotModifyVerified", "A verified expense is on the group's books and cannot be changed.");
}

public static class FixedDepositErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("FixedDeposit.NotFound", $"Fixed deposit with ID {id} was not found.");

    public static readonly Error NotInGroup =
        Error.Forbidden("FixedDeposit.NotInGroup", "Fixed deposit does not belong to this group.");

    public static readonly Error AlreadyClosed =
        Error.Problem("FixedDeposit.AlreadyClosed", "Fixed deposit is already matured or withdrawn.");

    public static readonly Error AlreadyWithdrawn =
        Error.Conflict("FixedDeposit.AlreadyWithdrawn", "This fixed deposit has already been withdrawn.");

    public static readonly Error NegativeInterest =
        Error.Problem("FixedDeposit.NegativeInterest", "Interest returned cannot be negative.");

    public static readonly Error WithdrawnBeforeStart =
        Error.Problem("FixedDeposit.WithdrawnBeforeStart", "A withdrawal cannot be dated before the deposit started.");

    public static readonly Error AlreadyVerified =
        Error.Conflict("FixedDeposit.AlreadyVerified", "This fixed deposit has already been verified.");

    public static readonly Error CannotModifyVerified =
        Error.Conflict("FixedDeposit.CannotModifyVerified", "A verified fixed deposit is on the group's books and cannot be changed or removed.");

    public static readonly Error NotVerified =
        Error.Problem("FixedDeposit.NotVerified", "This fixed deposit has not been verified yet, so it cannot be withdrawn.");

    public static readonly Error NotWithdrawn =
        Error.Problem("FixedDeposit.NotWithdrawn", "This fixed deposit has not been withdrawn, so there is no withdrawal to verify.");

    public static readonly Error WithdrawalAlreadyVerified =
        Error.Conflict("FixedDeposit.WithdrawalAlreadyVerified", "This withdrawal has already been verified.");
}

public static class OtherIncomingFundErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("OtherIncomingFund.NotFound", $"Incoming funds record with ID {id} was not found.");

    public static readonly Error NotInGroup =
        Error.Forbidden("OtherIncomingFund.NotInGroup", "This record does not belong to this group.");

    public static readonly Error AmountNotPositive =
        Error.Validation("OtherIncomingFund.AmountNotPositive", "The amount received must be more than zero.");

    public static readonly Error RemarksRequired =
        Error.Validation("OtherIncomingFund.RemarksRequired", "Remarks are required — they are what says which kind of income this was.");

    public static readonly Error AlreadyVerified =
        Error.Conflict("OtherIncomingFund.AlreadyVerified", "This record has already been verified.");

    public static readonly Error CannotModifyVerified =
        Error.Conflict("OtherIncomingFund.CannotModifyVerified", "A verified record is on the group's books and cannot be changed.");
}
