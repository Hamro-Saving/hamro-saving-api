using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.UpdateLoan;

internal sealed class UpdateLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateLoanCommand>
{
    public async Task<Result> Handle(UpdateLoanCommand command, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.IsSuperAdmin && loan.GroupId != userContext.GroupId)
            return Result.Failure(LoanErrors.NotInGroup);

        // Members can only edit their own loans; admins can edit any in their group
        if (!userContext.IsSuperAdmin && !userContext.IsAdmin && loan.BorrowerId != userContext.UserId)
            return Result.Failure(UserErrors.Unauthorized);

        var result = loan.Update(command.Amount, command.InterestRate, command.DueDate, command.Notes);
        if (result.IsFailure)
            return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
