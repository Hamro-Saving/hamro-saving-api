using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Ledger;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Loans.VerifyPayment;

internal sealed class VerifyLoanPaymentCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : ICommandHandler<VerifyLoanPaymentCommand>
{
    public async Task<Result> Handle(VerifyLoanPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsGroupAdmin)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        var payment = await dbContext.LoanPayments
            .FirstOrDefaultAsync(p => p.Id == command.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result.Failure(LoanErrors.PaymentNotFound(command.PaymentId));
        }

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == payment.LoanId, cancellationToken);

        if (loan is not null && !userContext.CanWrite(loan.GroupId))
        {
            return Result.Failure(LoanErrors.NotInGroup);
        }

        var result = payment.Verify(userContext.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        if (loan is not null)
        {
            dbContext.PostLoanPrincipal(loan.GroupId, payment.Id, loan.BorrowerId,
                payment.PrincipalAmount, payment.PaidDate, "Loan principal repaid");

            dbContext.PostLoanInterest(loan.GroupId, payment.Id, loan.BorrowerId,
                payment.InterestAmount, payment.PaidDate, "Loan interest received");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
