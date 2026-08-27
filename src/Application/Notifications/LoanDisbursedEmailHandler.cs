using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

internal sealed class LoanDisbursedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<LoanDisbursedDomainEvent>
{
    public async Task Handle(LoanDisbursedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == domainEvent.LoanId, cancellationToken);

        if (loan is null) return;

        var borrower = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == domainEvent.BorrowerId, cancellationToken);

        if (borrower is null) return;

        // Money leaving the group is the one thing nobody is excluded from.
        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = (await NotificationRecipients
                .Participants(dbContext.Members, domainEvent.GroupId)
                .ToRecipientsAsync(cancellationToken))
            .IncludingBorrower(borrower);

        if (recipients.Count == 0) return;

        await emailService.SendLoanDisbursedAsync(recipients, group, loan, borrower, cancellationToken);
    }
}
