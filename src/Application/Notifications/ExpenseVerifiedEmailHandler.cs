using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Finance;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Notifications;

/// <summary>An expense is the only outflow with no vote behind it, so this is the members' one sight of it.</summary>
internal sealed class ExpenseVerifiedEmailHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<ExpenseVerifiedDomainEvent>
{
    public async Task Handle(ExpenseVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == domainEvent.ExpenseId, cancellationToken);

        if (expense is null) return;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == domainEvent.GroupId, cancellationToken);

        if (group is null) return;

        var recipients = await NotificationRecipients
            .Participants(dbContext.Members, domainEvent.GroupId)
            .ExceptUser(expense.VerifiedById)
            .ToRecipientsAsync(cancellationToken);

        if (recipients.Count == 0) return;

        await emailService.SendExpenseVerifiedAsync(recipients, group, expense, cancellationToken);
    }
}
