using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Finance;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>The interest is the part worth reading: it is what the institution actually paid, not what was expected.</summary>
internal sealed class FixedDepositWithdrawalVerifiedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<FixedDepositWithdrawalVerifiedDomainEvent>
{
    public async Task Handle(FixedDepositWithdrawalVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var fixedDeposit = await dbContext.FixedDeposits
            .FirstOrDefaultAsync(fd => fd.Id == domainEvent.FixedDepositId, cancellationToken);

        if (fixedDeposit is null) return;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .ExceptUser(fixedDeposit.WithdrawalVerifiedById)
            .ToRecipientsAsync(cancellationToken);

        if (recipients.Count == 0) return;

        await emailService.SendFixedDepositWithdrawalVerifiedAsync(recipients, group, fixedDeposit, cancellationToken);
    }
}
