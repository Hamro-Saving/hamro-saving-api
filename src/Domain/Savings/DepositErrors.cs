using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Savings;

public static class DepositErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Deposit.NotFound", $"Deposit with ID {id} was not found.");

    public static readonly Error AlreadyVerified =
        Error.Conflict("Deposit.AlreadyVerified", "This deposit has already been verified.");

    public static readonly Error CannotModifyVerified =
        Error.Problem("Deposit.CannotModifyVerified", "Cannot modify a verified deposit.");

    public static readonly Error NotInGroup =
        Error.Forbidden("Deposit.NotInGroup", "Deposit does not belong to this group.");
}
