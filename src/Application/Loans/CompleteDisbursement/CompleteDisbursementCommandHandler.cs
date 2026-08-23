using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.CompleteDisbursement;

internal sealed class CompleteDisbursementCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<CompleteDisbursementCommand>
{
    public async Task<Result> Handle(CompleteDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsAdmin && !userContext.IsSuperAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.IsSuperAdmin && loan.GroupId != userContext.GroupId)
            return Result.Failure(LoanErrors.NotInGroup);

        var result = loan.CompleteDisbursement(userContext.UserId, DateTime.UtcNow);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
