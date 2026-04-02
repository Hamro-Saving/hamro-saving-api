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
}
