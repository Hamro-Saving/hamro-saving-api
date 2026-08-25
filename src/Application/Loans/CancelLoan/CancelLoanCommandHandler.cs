using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.CancelLoan;

internal sealed class CancelLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CancelLoanCommand>
{
    public async Task<Result> Handle(CancelLoanCommand command, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        // The person who asked for the loan may withdraw the request; an admin may pull
        // any of them. Either way only until the money has left.
        var isBorrower = loan.BorrowerType == "Member" && loan.BorrowerId == userContext.ActiveMemberId;

        if (!userContext.IsGroupAdmin && !isBorrower)
            return Result.Failure(UserErrors.Unauthorized);

        var result = loan.Cancel();
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
