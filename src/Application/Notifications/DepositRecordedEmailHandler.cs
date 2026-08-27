using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Savings;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>
/// Admins only: an unverified figure can still be corrected or deleted, and announcing one
/// that later changes is worse than announcing it a day late.
/// </summary>
internal sealed class DepositRecordedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<DepositRecordedDomainEvent>
{
    public async Task Handle(DepositRecordedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var deposit = await dbContext.Deposits
            .FirstOrDefaultAsync(d => d.Id == domainEvent.DepositId, cancellationToken);

        if (deposit is null) return;

        var depositor = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == domainEvent.MemberId, cancellationToken);

        if (depositor is null) return;

        // Nobody is excluded: an admin who recorded their own deposit still has to verify it.
        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var admins = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .AdminsOnly()
            .ToRecipientsAsync(cancellationToken);

        if (admins.Count == 0) return;

        await emailService.SendDepositRecordedAsync(admins, group, deposit, depositor, cancellationToken);
    }
}
