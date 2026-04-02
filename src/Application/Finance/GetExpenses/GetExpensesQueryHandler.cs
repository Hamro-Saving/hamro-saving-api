using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Finance.GetExpenses;

internal sealed class GetExpensesQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : IQueryHandler<GetExpensesQuery, List<ExpenseResponse>>
{
    public async Task<Result<List<ExpenseResponse>>> Handle(GetExpensesQuery query, CancellationToken cancellationToken = default)
    {
        var expensesQuery = dbContext.Expenses.AsQueryable();

        if (!userContext.IsSuperAdmin)
        {
            var groupId = userContext.GroupId;
            expensesQuery = expensesQuery.Where(e => e.GroupId == groupId);
        }
        else if (query.GroupId.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.GroupId == query.GroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            expensesQuery = expensesQuery.Where(e => e.Category == query.Category);
        }

        if (query.FromDate.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= query.ToDate.Value);
        }

        var expenses = await expensesQuery
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseResponse(
                e.Id,
                e.GroupId,
                e.Amount,
                e.Category,
                e.Description,
                e.ExpenseDate,
                e.ApprovedById,
                e.CreatedById,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(expenses);
    }
}
