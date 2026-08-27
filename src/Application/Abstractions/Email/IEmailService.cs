using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;

namespace HamroSavings.Application.Abstractions.Email;

public interface IEmailService
{
    /// <summary>
    /// Invites one person to create their account. Unlike every other method here this one
    /// <em>throws</em> if the send fails, because it has a caller who can act on that: resending
    /// an invite exists to send this email, and reporting success when it never went would leave
    /// an admin waiting for someone who was never asked.
    /// </summary>
    /// <param name="inviteToken">
    /// The token the signup link is built from. The link itself is assembled here, so the
    /// frontend's address is known in one place rather than in every caller.
    /// </param>
    Task SendMemberInviteAsync(EmailRecipient recipient, Group group, Guid inviteToken, CancellationToken ct = default);

    // --- Savings
    /// <summary>To the group's admins: a deposit has been entered and needs checking.</summary>
    Task SendDepositRecordedAsync(IReadOnlyCollection<EmailRecipient> admins, Group group, Deposit deposit, Member depositor, CancellationToken ct = default);

    /// <summary>To the group: a deposit is on the books.</summary>
    Task SendDepositVerifiedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, Deposit deposit, Member depositor, CancellationToken ct = default);

    // --- Loans
    /// <summary>To the members whose vote the loan is waiting on.</summary>
    Task SendLoanRequestedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, CancellationToken ct = default);

    Task SendLoanVoteSettledAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, bool isApproved, CancellationToken ct = default);

    Task SendLoanDisbursedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, CancellationToken ct = default);

    /// <summary>To the group's admins: a repayment has been entered and needs checking.</summary>
    Task SendLoanPaymentRecordedAsync(IReadOnlyCollection<EmailRecipient> admins, Group group, LoanPayment payment, Loan loan, Member borrower, CancellationToken ct = default);

    Task SendLoanPaymentVerifiedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, LoanPayment payment, Loan loan, Member borrower, CancellationToken ct = default);

    /// <summary>To the group: the loan is settled and closed.</summary>
    Task SendLoanPaidOffAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, Loan loan, Member borrower, LoanPayment finalPayment, CancellationToken ct = default);


    // --- Finance. These are told to the group only once they are on the books.

    Task SendExpenseVerifiedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, Expense expense, CancellationToken ct = default);

    Task SendFixedDepositVerifiedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, FixedDeposit fixedDeposit, CancellationToken ct = default);

    Task SendFixedDepositWithdrawalVerifiedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, FixedDeposit fixedDeposit, CancellationToken ct = default);

    Task SendOtherIncomingFundVerifiedAsync(IReadOnlyCollection<EmailRecipient> recipients, Group group, OtherIncomingFund record, Member payer, CancellationToken ct = default);
}
