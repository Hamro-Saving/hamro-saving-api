using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>
/// The borrower has no vote on their own request, so excluding them leaves exactly the
/// members the loan is waiting on.
/// </summary>
internal sealed class LoanRequestedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<LoanRequestedDomainEvent>
{
    public async Task Handle(LoanRequestedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var loan = await dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == domainEvent.LoanId, cancellationToken);

        if (loan is null) return;

        var borrower = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == domainEvent.BorrowerId, cancellationToken);

        if (borrower is null) return;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .ExceptMember(domainEvent.BorrowerId)
            .ToRecipientsAsync(cancellationToken);

        if (recipients.Count == 0) return;

        await emailService.SendLoanRequestedAsync(recipients, group, loan, borrower, cancellationToken);
    }
}
