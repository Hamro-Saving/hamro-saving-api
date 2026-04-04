using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
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
        if (!userContext.IsSuperAdmin && userContext.GroupId != command.GroupId)
        {
            return Result.Failure<Guid>(UserErrors.NotInGroup);
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<Guid>(GroupErrors.NotFound(command.GroupId));
        }

        var expense = Expense.Create(
            command.GroupId,
            command.Amount,
            command.Category,
            command.Description,
            command.ExpenseDate,
            userContext.UserId);

        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(expense.Id);
    }
}
