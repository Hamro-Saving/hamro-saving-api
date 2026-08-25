using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.DeleteLoan;

/// <summary>
/// Clears away a loan that came to nothing. Only once cancelled: any loan that reached
/// disbursement has entries in the books, and those are corrected by an opposite entry
/// rather than by deleting what they refer to.
/// </summary>
internal sealed class DeleteLoanCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteLoanCommand>
{
    public async Task<Result> Handle(DeleteLoanCommand command, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(command.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        // Whoever could cancel it can clear it away: the borrower, or an admin.
        var isBorrower = loan.BorrowerType == "Member" && loan.BorrowerId == userContext.ActiveMemberId;

        if (!userContext.IsGroupAdmin && !isBorrower)
            return Result.Failure(UserErrors.Unauthorized);

        if (loan.Status != LoanStatus.Cancelled)
            return Result.Failure(LoanErrors.CannotDeleteUnlessCancelled);

        // Belt and braces: a cancelled loan never reached disbursement, so it should
        // have no entries — and if it does, deleting it would orphan the books.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "Loan" && e.SourceId == loan.Id, cancellationToken);

        if (inLedger)
            return Result.Failure(LoanErrors.InLedger);

        // Votes are not tied to the loan by a foreign key, so they go explicitly.
        var votes = await dbContext.LoanApprovals
            .Where(a => a.LoanId == loan.Id)
            .ToListAsync(cancellationToken);

        dbContext.LoanApprovals.RemoveRange(votes);
        dbContext.Loans.Remove(loan);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
