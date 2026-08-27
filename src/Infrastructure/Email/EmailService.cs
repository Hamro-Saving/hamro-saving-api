using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Abstractions.Settings;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HamroSavings.Infrastructure.Email;

/// <summary>
/// One method per email. Each decides its own wording; <see cref="EmailLayout"/> makes them
/// all look alike, and <see cref="Deliver"/> gets them out.
/// </summary>
internal sealed class EmailService(
    IEmailSender sender,
    ILogger<EmailService> logger,
    IOptions<FrontendSettings> frontendSettings)
    : IEmailService
{
    public Task SendMemberInviteAsync(EmailRecipient recipient, Group group, Guid inviteToken, CancellationToken ct = default) =>
        // Not via Deliver: the caller is waiting to be told whether it worked, so a failure
        // must escape rather than be swallowed.
        SendOneAsync(
            recipient,
            group.Name,
            subject: $"You've been invited to {group.Name}",
            headline: $"You have been added as a member of {group.Name}.",
            details: [],
            footnote: "This link expires in 72 hours. If you did not expect this email, you can safely ignore it.",
            actionLabel: "Create my account",
            link: Absolute(EmailLinks.Signup(inviteToken)),
            ct: ct);

    // ------------------------------------------------------------------ Savings

    public Task SendDepositRecordedAsync(
        IReadOnlyCollection<EmailRecipient> admins, Group group, Deposit deposit, Member depositor, CancellationToken ct = default)
    {
        var what = DepositNarrative.Describe(deposit);

        return Deliver(
            admins,
            group.Name,
            subject: $"Verify: {what} from {depositor.FullName}",
            headline: $"{depositor.FullName} has recorded a {what}.",
            details: DepositNarrative.Figures(deposit, depositor.FullName),
            footnote: "It is not on the group's books yet and the other members have not been told. Verifying it posts it to the ledger and notifies the group.",
            // Only reached when the depositor is an admin, and so among the recipients.
            self: SelfAddressed.To(
                depositor.Email,
                $"Your {what} needs verifying",
                $"You have recorded a {what}."),
            ct: ct);
    }

    public Task SendDepositVerifiedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, Deposit deposit, Member depositor, CancellationToken ct = default)
    {
        var what = DepositNarrative.Describe(deposit);

        return Deliver(
            recipients,
            group.Name,
            subject: $"{depositor.FullName} has made a {what}",
            headline: $"{depositor.FullName} has made a {what}.",
            details: DepositNarrative.Figures(deposit, depositor.FullName),
            self: SelfAddressed.To(
                depositor.Email,
                $"Your {what} has been verified",
                $"Your {what} has been verified and is on the group's books."),
            ct: ct);
    }

    // ------------------------------------------------------------------ Loans

    public Task SendLoanRequestedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, CancellationToken ct = default)
    {
        var who = LoanNarrative.Borrower(borrower);

        return Deliver(
            recipients,
            group.Name,
            subject: $"Your vote is needed: loan of {Money.Format(loan.Amount)} for {who}",
            headline: $"{who} has requested a loan of {Money.Format(loan.Amount)} from the group.",
            details: LoanNarrative.Terms(loan, borrower),
            footnote: "The loan cannot be paid out until the members have voted. Please approve or decline it.",
            // The only email that asks the reader to go and do something, so the only one with a link.
            actionLabel: "Approve or decline",
            actionPath: EmailLinks.Loan(loan.Id),
            ct: ct);
    }

    public Task SendLoanVoteSettledAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, bool isApproved, CancellationToken ct = default)
    {
        var who = LoanNarrative.Borrower(borrower);
        var outcome = isApproved ? "approved" : "declined";

        return Deliver(
            recipients,
            group.Name,
            subject: $"Loan {outcome}: {Money.Format(loan.Amount)} for {who}",
            headline: $"The group has {outcome} {who}'s loan of {Money.Format(loan.Amount)}.",
            details: LoanNarrative.Terms(loan, borrower),
            footnote: isApproved
                ? "The money has not left the group yet. An admin will pay it out."
                : "The request is closed and no money will be paid out.",
            self: SelfAddressed.To(
                borrower.Email,
                $"Your loan has been {outcome}",
                $"The group has {outcome} your loan of {Money.Format(loan.Amount)}."),
            ct: ct);
    }

    public Task SendLoanDisbursedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, CancellationToken ct = default)
    {
        var who = LoanNarrative.Borrower(borrower);

        var details = LoanNarrative.Terms(loan, borrower);
        details.Insert(1, new EmailDetail("Amount paid out", Money.Format(loan.Amount)));
        if (loan.WasReducedAtDisbursement)
            details.Insert(2, new EmailDetail("Amount approved", Money.Format(loan.RequestedAmount)));
        if (loan.DisbursedAt is { } on)
            details.Add(new EmailDetail("Paid out on", on.ToString("dd MMM yyyy")));

        return Deliver(
            recipients,
            group.Name,
            subject: $"Loan paid out: {Money.Format(loan.Amount)} to {who}",
            headline: $"{Money.Format(loan.Amount)} has been paid out to {who} and interest is now running on it.",
            details: details,
            // Called out rather than left in the figures — these two change what the payout means.
            footnote: Footnotes.Join(
                loan.IsForceDisbursed
                    ? "This loan was paid out by an admin without a completed vote by the members."
                    : null,
                loan.WasReducedAtDisbursement
                    ? $"Less was handed over than the group approved: {Money.Format(loan.Amount)} against {Money.Format(loan.RequestedAmount)}. The loan is now for the smaller figure."
                    : null),
            self: SelfAddressed.To(
                borrower.Email,
                "Your loan has been paid out",
                $"{Money.Format(loan.Amount)} has been paid out to you and interest is now running on it."),
            ct: ct);
    }

    public Task SendLoanPaymentRecordedAsync(
        IReadOnlyCollection<EmailRecipient> admins, Group group, LoanPayment payment, Loan loan, Member borrower, CancellationToken ct = default)
    {
        var who = LoanNarrative.Borrower(borrower);
        var what = LoanPaymentNarrative.Describe(payment);

        return Deliver(
            admins,
            group.Name,
            subject: $"Verify: {what} from {borrower.FullName}",
            headline: $"A {what} has been recorded against {who}'s loan.",
            details: LoanPaymentNarrative.Figures(payment, loan, who),
            footnote: Footnotes.Join(
                LoanPaymentNarrative.InterestStillOwed(payment),
                "It is not on the group's books yet and the other members have not been told. Verifying it posts it to the ledger and notifies the group."),
            self: SelfAddressed.To(
                borrower.Email,
                $"Your {what} needs verifying",
                $"A {what} has been recorded against your loan."),
            ct: ct);
    }

    public Task SendLoanPaymentVerifiedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, LoanPayment payment, Loan loan, Member borrower, CancellationToken ct = default)
    {
        var who = LoanNarrative.Borrower(borrower);
        var what = LoanPaymentNarrative.Describe(payment);

        return Deliver(
            recipients,
            group.Name,
            subject: $"{borrower.FullName} has made a {what}",
            headline: $"{who} has made a {what} against their loan.",
            details: LoanPaymentNarrative.Figures(payment, loan, who),
            footnote: LoanPaymentNarrative.InterestStillOwed(payment),
            self: SelfAddressed.To(
                borrower.Email,
                $"Your {what} has been verified",
                $"Your {what} has been verified and is on the group's books."),
            ct: ct);
    }

    public Task SendLoanPaidOffAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, LoanPayment finalPayment, CancellationToken ct = default)
    {
        var who = LoanNarrative.Borrower(borrower);

        return Deliver(
            recipients,
            group.Name,
            subject: $"Loan fully repaid: {who}",
            headline: $"{who}'s loan is now fully repaid and closed.",
            details:
            [
                new EmailDetail("Borrower", who),
                new EmailDetail("Principal repaid", Money.Format(loan.TotalPrincipalPaid)),
                new EmailDetail("Interest earned by the group", Money.Format(loan.TotalInterestPaid)),
                new EmailDetail("Interest rate", $"{loan.InterestRate:0.##}% per year"),
                .. loan.DisbursedAt is { } on
                    ? new[] { new EmailDetail("Paid out on", on.ToString("dd MMM yyyy")) }
                    : [],
                new EmailDetail("Settled on", finalPayment.PaidDate.ToString("dd MMM yyyy")),
            ],
            self: SelfAddressed.To(
                borrower.Email,
                "Your loan is fully repaid",
                "Your loan is now fully repaid and closed. Nothing further is owed on it."),
            ct: ct);
    }

    // ------------------------------------------------------------------ Finance

    public Task SendExpenseVerifiedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, Expense expense, CancellationToken ct = default) =>
        Deliver(
            recipients,
            group.Name,
            subject: $"Group expense: {Money.Format(expense.Amount)} — {expense.Category}",
            headline: $"The group has spent {Money.Format(expense.Amount)} on {expense.Category}.",
            details:
            [
                new EmailDetail("Amount", Money.Format(expense.Amount)),
                new EmailDetail("Category", expense.Category),
                new EmailDetail("What for", expense.Description),
                new EmailDetail("Spent on", expense.ExpenseDate.ToString("dd MMM yyyy")),
            ],
            ct: ct);

    public Task SendFixedDepositVerifiedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, FixedDeposit fixedDeposit, CancellationToken ct = default) =>
        Deliver(
            recipients,
            group.Name,
            subject: $"Fixed deposit placed: {Money.Format(fixedDeposit.Amount)} with {fixedDeposit.InstitutionName}",
            headline: $"{Money.Format(fixedDeposit.Amount)} of the group's money has been placed as a fixed deposit with {fixedDeposit.InstitutionName}.",
            details:
            [
                new EmailDetail("Institution", fixedDeposit.InstitutionName),
                new EmailDetail("Amount", Money.Format(fixedDeposit.Amount)),
                new EmailDetail("Interest rate", $"{fixedDeposit.InterestRate:0.##}% per year"),
                new EmailDetail("Placed on", fixedDeposit.StartDate.ToString("dd MMM yyyy")),
                new EmailDetail("Matures on", fixedDeposit.MaturityDate.ToString("dd MMM yyyy")),
                new EmailDetail("Expected at maturity", Money.Format(fixedDeposit.ExpectedMaturityAmount)),
                .. string.IsNullOrWhiteSpace(fixedDeposit.Notes)
                    ? []
                    : new[] { new EmailDetail("Notes", fixedDeposit.Notes!) },
            ],
            footnote: "This money is with the institution until it matures, so it is no longer part of the group's cash in hand.",
            ct: ct);

    public Task SendFixedDepositWithdrawalVerifiedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, FixedDeposit fixedDeposit, CancellationToken ct = default)
    {
        var interest = fixedDeposit.InterestEarned ?? 0;
        var total = fixedDeposit.Amount + interest;

        return Deliver(
            recipients,
            group.Name,
            subject: $"Fixed deposit withdrawn: {Money.Format(total)} from {fixedDeposit.InstitutionName}",
            headline: $"{Money.Format(total)} has come back to the group from {fixedDeposit.InstitutionName}, including {Money.Format(interest)} of interest earned.",
            details:
            [
                new EmailDetail("Institution", fixedDeposit.InstitutionName),
                new EmailDetail("Principal returned", Money.Format(fixedDeposit.Amount)),
                new EmailDetail("Interest earned", Money.Format(interest)),
                new EmailDetail("Total received", Money.Format(total)),
                .. fixedDeposit.WithdrawnAt is { } on
                    ? new[] { new EmailDetail("Withdrawn on", on.ToString("dd MMM yyyy")) }
                    : [],
            ],
            // A short return is normal for an early closure, and the one figure a member might think wrong.
            footnote: total < fixedDeposit.ExpectedMaturityAmount
                ? $"This is less than the {Money.Format(fixedDeposit.ExpectedMaturityAmount)} expected at maturity, which is usual when a deposit is closed before its maturity date."
                : null,
            ct: ct);
    }

    public Task SendOtherIncomingFundVerifiedAsync(
        IReadOnlyCollection<EmailRecipient> recipients, Group group, OtherIncomingFund record, Member payer, CancellationToken ct = default) =>
        Deliver(
            recipients,
            group.Name,
            // The remark is the only thing identifying which kind of income this was.
            subject: $"{payer.FullName} has paid {Money.Format(record.Amount)} to the group",
            headline: $"{payer.FullName} has paid {Money.Format(record.Amount)} to the group — {record.Remarks}.",
            details:
            [
                new EmailDetail("Member", payer.FullName),
                new EmailDetail("Amount", Money.Format(record.Amount)),
                new EmailDetail("What for", record.Remarks),
                new EmailDetail("Paid on", record.PaidDate.ToString("dd MMM yyyy")),
            ],
            footnote: "This is income to the group rather than savings, so it is not owed back.",
            self: SelfAddressed.To(
                payer.Email,
                $"Your payment of {Money.Format(record.Amount)} has been verified",
                $"Your payment of {Money.Format(record.Amount)} — {record.Remarks} — has been verified and is on the group's books."),
            ct: ct);

    // ------------------------------------------------------------------ Delivery

    /// <summary>The subject of the event reads it in the second person; everyone else gets the group's account.</summary>
    private async Task Deliver(
        IReadOnlyCollection<EmailRecipient> recipients,
        string groupName,
        string subject,
        string headline,
        IReadOnlyList<EmailDetail> details,
        string? footnote = null,
        SelfAddressed? self = null,
        string? actionLabel = null,
        string? actionPath = null,
        CancellationToken ct = default)
    {
        var link = Absolute(actionPath);

        foreach (var recipient in recipients)
        {
            ct.ThrowIfCancellationRequested();

            var (lineSubject, lineHeadline) = EmailLayout.For(recipient, subject, headline, self);

            try
            {
                await SendOneAsync(recipient, groupName, lineSubject, lineHeadline, details, footnote, actionLabel, link, ct);
            }
            catch (Exception exception)
            {
                // One unreachable address is not a reason to abandon the rest of the group.
                logger.LogError(exception, "Failed to send notification {Subject} to {Email}", lineSubject, recipient.Email);
            }
        }
    }

    /// <summary>Failure is the caller's to handle.</summary>
    private Task SendOneAsync(
        EmailRecipient recipient,
        string groupName,
        string subject,
        string headline,
        IReadOnlyList<EmailDetail> details,
        string? footnote,
        string? actionLabel,
        string? link,
        CancellationToken ct) =>
        sender.SendAsync(
            recipient.Email,
            fromName: groupName,
            subject,
            EmailLayout.Html(recipient, groupName, headline, details, footnote, actionLabel, link),
            EmailLayout.Text(recipient, groupName, headline, details, footnote, actionLabel, link),
            ct);

    private string? Absolute(string? path) =>
        path is null ? null : frontendSettings.Value.Url.TrimEnd('/') + path;
}
