using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Savings;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

internal sealed class DepositVerifiedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<DepositVerifiedDomainEvent>
{
    public async Task Handle(DepositVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == domainEvent.DepositId, cancellationToken);

        if (deposit is null) return;

        var depositor = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == domainEvent.MemberId, cancellationToken);

        if (depositor is null) return;

        // Only the admin who just verified it is left out.
        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .ExceptUser(deposit.VerifiedById)
            .ToRecipientsAsync(cancellationToken);

        if (recipients.Count == 0) return;

        await emailService.SendDepositVerifiedAsync(recipients, group, deposit, depositor, cancellationToken);
    }
}
