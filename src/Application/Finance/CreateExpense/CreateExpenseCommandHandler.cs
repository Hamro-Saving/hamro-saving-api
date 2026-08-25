using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Ledger;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Finance.CreateExpense;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.CreateExpense;

internal sealed class CreateExpenseCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CreateExpenseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        // Admins and members act in the group on their token; only a SuperAdmin names one
        var groupResult = userContext.ResolveWriteGroupId();
        if (groupResult.IsFailure) return Result.Failure<Guid>(groupResult.Error);
        var groupId = groupResult.Value;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(groupId));
        }


        // The rule about what the group may commit lives on CashInHand; the balance is
        // read from the books here.
        var inHand = await CashPosition.InHandAsync(dbContext, groupId, cancellationToken);
        var covered = inHand.EnsureCovers(command.Amount);
        if (covered.IsFailure) return Result.Failure<Guid>(covered.Error);

        var expense = Expense.Create(
            groupId,
            command.Amount,
            command.Category,
            command.Description,
            command.ExpenseDate,
            userContext.UserId);

        dbContext.Expenses.Add(expense);
        dbContext.PostExpense(expense.GroupId, expense.Id, expense.Amount, expense.ExpenseDate,
            $"{expense.Category}: {expense.Description}");
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(expense.Id);
    }
}
