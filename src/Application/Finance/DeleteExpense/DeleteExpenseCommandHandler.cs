using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.DeleteExpense;

/// <summary>
/// Removes an expense entered in error. Only while unverified: once verified the money is
/// spent in the group's books, and a ledger entry is never unwritten — correcting that is an
/// opposite entry, not a deletion.
/// </summary>
internal sealed class DeleteExpenseCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteExpenseCommand>
{
    public async Task<Result> Handle(DeleteExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == command.ExpenseId, cancellationToken);

        if (expense is null)
            return Result.Failure(ExpenseErrors.NotFound(command.ExpenseId));

        if (!userContext.CanWrite(expense.GroupId))
            return Result.Failure(ExpenseErrors.NotInGroup);

        if (expense.IsVerified)
            return Result.Failure(ExpenseErrors.CannotModifyVerified);

        // Belt and braces: an unverified expense should have no entries, and if one somehow
        // exists then deleting the record would orphan the books.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "Expense" && e.SourceId == expense.Id, cancellationToken);

        if (inLedger)
            return Result.Failure(ExpenseErrors.CannotModifyVerified);

        dbContext.Expenses.Remove(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
