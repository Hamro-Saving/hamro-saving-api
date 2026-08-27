using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>
/// Closure rides on this rather than being its own event: a loan is only settled once the
/// money behind the final payment has been verified.
/// </summary>
internal sealed class LoanPaymentVerifiedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<LoanPaymentVerifiedDomainEvent>
{
    public async Task Handle(LoanPaymentVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var context = await LoanPaymentContext.LoadAsync(dbContext, domainEvent.PaymentId, domainEvent.LoanId, cancellationToken);
        if (context is null) return;

        var (payment, loan, borrower, group) = context;

        // Everyone but the verifying admin; the borrower is included, non-member or not.
        var recipients = (await NotificationRecipients
                .Participants(dbContext.Members, loan.GroupId)
                .ExceptUser(payment.VerifiedById)
                .ToRecipientsAsync(cancellationToken))
            .IncludingBorrower(borrower, exceptUserId: payment.VerifiedById);

        if (recipients.Count == 0) return;

        await emailService.SendLoanPaymentVerifiedAsync(recipients, group, payment, loan, borrower, cancellationToken);

        if (await IsSettledAsync(loan, cancellationToken))
        {
            await emailService.SendLoanPaidOffAsync(recipients, group, loan, borrower, payment, cancellationToken);
        }
    }

    /// <summary>
    /// Status alone is not enough: it flips to PaidOff when the final payment is
    /// <em>recorded</em>, so announcing closure then would call the books settled when an
    /// unverified payment is still outstanding.
    /// </summary>
    private async Task<bool> IsSettledAsync(Loan loan, CancellationToken cancellationToken) =>
        loan.Status == LoanStatus.PaidOff &&
        !await dbContext.LoanPayments.AnyAsync(p => p.LoanId == loan.Id && !p.IsVerified, cancellationToken);
}
