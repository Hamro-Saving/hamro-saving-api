using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

public static class ExpenseErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Expense.NotFound", $"Expense with ID {id} was not found.");

    public static readonly Error NotInGroup =
        Error.Forbidden("Expense.NotInGroup", "Expense does not belong to this group.");
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
}
