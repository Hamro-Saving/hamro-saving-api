using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Finance;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

internal sealed class FixedDepositVerifiedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<FixedDepositVerifiedDomainEvent>
{
    public async Task Handle(FixedDepositVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == domainEvent.FixedDepositId, cancellationToken);

        if (fixedDeposit is null) return;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .ExceptUser(fixedDeposit.VerifiedById)
            .ToRecipientsAsync(cancellationToken);

        if (recipients.Count == 0) return;

        await emailService.SendFixedDepositVerifiedAsync(recipients, group, fixedDeposit, cancellationToken);
    }
}
