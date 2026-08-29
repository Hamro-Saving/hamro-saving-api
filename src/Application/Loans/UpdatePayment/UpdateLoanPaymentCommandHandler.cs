using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.UpdatePayment;

/// <summary>
/// Fixes a payment entered wrongly — the wrong figure, the wrong day, the wrong split. Only
/// while it is unverified: once verified it is in the group's books, and a ledger entry is
/// corrected by an opposite entry rather than by rewriting what it refers to.
/// </summary>
internal sealed class UpdateLoanPaymentCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateLoanPaymentCommand>
{
    public async Task<Result> Handle(UpdateLoanPaymentCommand command, CancellationToken cancellationToken = default)
    {
        // Recording a payment is the admin's job, so correcting one is too.
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

        if (command.PaidDate.Date > DateTime.UtcNow.Date)
            return Result.Failure(LoanErrors.PaymentInFuture);

        var payments = await dbContext.LoanPayments
            .Where(p => p.LoanId == loan.Id)
            .ToListAsync(cancellationToken);

        if (LoanPaymentReplay.HasVerifiedPaymentAfter(payment, payments))
            return Result.Failure(LoanErrors.VerifiedPaymentAfter);

        var revised = payment.Revise(command.PaidDate, command.PrincipalAmount, command.InterestAmount, command.Notes);
        if (revised.IsFailure)
            return revised;

        // The loan is wound back and every payment applied again, so the correction carries
        // through to the interest that ran after it.
        var replayed = LoanPaymentReplay.Apply(loan, payments);
        if (replayed.IsFailure)
            return replayed;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
