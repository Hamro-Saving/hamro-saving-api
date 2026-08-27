using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Finance;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

internal sealed class OtherIncomingFundVerifiedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<OtherIncomingFundVerifiedDomainEvent>
{
    public async Task Handle(OtherIncomingFundVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.OtherIncomingFunds
            .FirstOrDefaultAsync(r => r.Id == domainEvent.RecordId, cancellationToken);

        if (record is null) return;

        var payer = await dbContext.Members
            .FirstOrDefaultAsync(m => m.Id == domainEvent.MemberId, cancellationToken);

        if (payer is null) return;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .ExceptUser(record.VerifiedById)
            .ToRecipientsAsync(cancellationToken);

        if (recipients.Count == 0) return;

        await emailService.SendOtherIncomingFundVerifiedAsync(recipients, group, record, payer, cancellationToken);
    }
}
