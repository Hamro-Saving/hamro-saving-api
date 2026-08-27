using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>
/// The four rows every repayment email needs. A notification handler runs on its own
/// connection after the fact, so it must fetch whatever it wants; loading them together keeps
/// the two payment handlers from repeating the same queries and guards.
/// </summary>
internal sealed record LoanPaymentContext(LoanPayment Payment, Loan Loan, Member Borrower, Group Group)
{
    public static async Task<LoanPaymentContext?> LoadAsync(
        IApplicationDbContext dbContext,
        Guid paymentId,
        Guid loanId,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.LoanPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null) return null;

        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan is null) return null;

        var borrower = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == loan.BorrowerId, cancellationToken);

        if (borrower is null) return null;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == loan.GroupId, cancellationToken);

        return group is null ? null : new LoanPaymentContext(payment, loan, borrower, group);
    }
}
