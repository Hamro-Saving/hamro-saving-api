using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Ledger;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.VerifyExpense;

internal sealed class VerifyExpenseCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyExpenseCommand>
{
    public async Task<Result> Handle(VerifyExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == command.ExpenseId, cancellationToken);

        if (expense is null)
            return Result.Failure(ExpenseErrors.NotFound(command.ExpenseId));

        if (!userContext.CanWrite(expense.GroupId))
            return Result.Failure(ExpenseErrors.NotInGroup);

        // Checked here, not at recording: an unverified expense commits nothing, so several
        // can be entered against the same balance.
        var inHand = await CashPosition.InHandAsync(dbContext, expense.GroupId, cancellationToken);
        var covered = inHand.EnsureCovers(expense.Amount);
        if (covered.IsFailure) return covered;

        var result = expense.Verify(userContext.UserId);
        if (result.IsFailure) return result;

        dbContext.PostExpense(expense.GroupId, expense.Id, expense.Amount, expense.ExpenseDate,
            $"{expense.Category}: {expense.Description}");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
