using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.DeletePayment;

/// <summary>
/// Removes a payment that was recorded in error. Only while it is unverified: once verified
/// the money is in the group's books, and a ledger entry is never unwritten — correcting that
/// is an opposite entry, not a deletion.
/// </summary>
internal sealed class DeleteLoanPaymentCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteLoanPaymentCommand>
{
    public async Task<Result> Handle(DeleteLoanPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
            return Result.Failure(UserErrors.Unauthorized);

        var payment = await dbContext.LoanPayments
            .FirstOrDefaultAsync(p => p.Id == command.PaymentId, cancellationToken);

        if (payment is null)
            return Result.Failure(LoanErrors.PaymentNotFound(command.PaymentId));

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == payment.LoanId, cancellationToken);

        if (loan is null)
            return Result.Failure(LoanErrors.NotFound(payment.LoanId));

        if (!userContext.CanWrite(loan.GroupId))
            return Result.Failure(LoanErrors.NotInGroup);

        if (payment.IsVerified)
            return Result.Failure(LoanErrors.CannotModifyVerifiedPayment);

        // Belt and braces: an unverified payment should have no entries, and if one somehow
        // exists then deleting the record would orphan the books.
        var inLedger = await dbContext.LedgerEntries
            .AnyAsync(e => e.SourceType == "LoanPayment" && e.SourceId == payment.Id, cancellationToken);

        if (inLedger)
            return Result.Failure(LoanErrors.InLedger);

        var payments = await dbContext.LoanPayments
            .Where(p => p.LoanId == loan.Id)
            .ToListAsync(cancellationToken);

        if (LoanPaymentReplay.HasVerifiedPaymentAfter(payment, payments))
            return Result.Failure(LoanErrors.VerifiedPaymentAfter);

        payments.RemoveAll(p => p.Id == payment.Id);

        // Whatever this payment settled has to come back — the interest it cleared runs again,
        // and the principal it retired is owed once more — so the loan is rebuilt without it.
        var replayed = LoanPaymentReplay.Apply(loan, payments);
        if (replayed.IsFailure)
            return replayed;

        dbContext.LoanPayments.Remove(payment);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
