using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>Admins only, for the same reason a recorded deposit is: it is not on the books yet.</summary>
internal sealed class LoanPaymentRecordedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<LoanPaymentRecordedDomainEvent>
{
    public async Task Handle(LoanPaymentRecordedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var context = await LoanPaymentContext.LoadAsync(dbContext, domainEvent.PaymentId, domainEvent.LoanId, cancellationToken);
        if (context is null) return;

        var (payment, loan, borrower, group) = context;

        var admins = await NotificationRecipients
            .Participants(dbContext.Members, loan.GroupId)
            .AdminsOnly()
            .ToRecipientsAsync(cancellationToken);

        if (admins.Count == 0) return;

        await emailService.SendLoanPaymentRecordedAsync(admins, group, payment, loan, borrower, cancellationToken);
    }
}
