using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Ledger;
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
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        var inHand = await CashPosition.InHandAsync(dbContext, loan.GroupId, cancellationToken);

        // A date with no time of day: the payout is recorded as of midnight UTC on that day,
        // which is what the interest clock counts in.
        var disbursedAt = command.DisbursedOn is { } on
            ? on.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var result = loan.CompleteDisbursement(userContext.UserId, disbursedAt, inHand, command.DisbursedAmount);
        if (result.IsFailure) return result;

        dbContext.PostLoanDisbursement(loan.GroupId, loan.Id, loan.BorrowerId, loan.Amount,
            loan.DisbursedAt ?? DateTime.UtcNow, "Loan disbursed");

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
