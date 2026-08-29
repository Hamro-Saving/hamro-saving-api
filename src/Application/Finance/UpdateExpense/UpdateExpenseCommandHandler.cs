using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.UpdateExpense;

/// <summary>
/// Corrects an expense before it is verified. Nothing is checked against the group's cash
/// here: an unverified expense commits nothing, and verification is where the balance has
/// to cover it.
/// </summary>
internal sealed class UpdateExpenseCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateExpenseCommand>
{
    public async Task<Result> Handle(UpdateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == command.ExpenseId, cancellationToken);

        if (expense is null)
            return Result.Failure(ExpenseErrors.NotFound(command.ExpenseId));

        if (!userContext.CanWrite(expense.GroupId))
            return Result.Failure(ExpenseErrors.NotInGroup);

        var result = expense.Update(command.Amount, command.Category, command.Description, command.ExpenseDate);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
