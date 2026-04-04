using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.VerifyLoan;

internal sealed class VerifyLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyLoanCommand>
{
    public async Task<Result> Handle(VerifyLoanCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsAdmin && !userContext.IsSuperAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.IsSuperAdmin && loan.GroupId != userContext.GroupId)
            return Result.Failure(LoanErrors.NotInGroup);

        var result = loan.Verify(userContext.UserId);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
