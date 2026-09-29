# Domain rules

These rules are non-obvious and span the codebase. Read this before changing money flows.

## Groups, members, users

- A **User** is a login. A **Member** is that person's standing inside one **Group**. One user
  can have memberships in several groups, and the token carries the active one.
- A SuperAdmin user has no Member record.
- Leaving a group **deactivates** the member (`SetActive`) instead of deleting it, because
  their history is part of the books. The login is disabled once no active membership is
  left. The last admin of a group cannot be removed.
- Groups have a validity period (`SetGroupValidity`).

## Verification lifecycle

Every money record (deposit, loan payment, expense, fixed deposit and withdrawal, other
incoming fund) follows this lifecycle:

1. **Recorded, unverified:** a claim about money. It can be updated or deleted freely.
2. **Verified by a group admin:** now it is in the books. This is where the ledger entry is
   posted and the "on the books" email goes out.
3. **After verification:** it is immutable. Corrections are new opposite entries, and updates
   return `CannotModifyVerified` or a similar error.

Summaries and reports filter on `IsVerified`.

## Ledger (`Domain/Ledger`, `Application/Ledger`)

- Double-entry: each `LedgerEntry` has a debit account, a credit account, and an amount
  greater than 0, and the two accounts must differ.
- Accounts: `Cash`, `MemberSavings`, `LoanReceivable`, `InterestIncome`, `FixedDeposits`,
  `Expenses`.
- `LedgerPosting` is the **only** place that maps a business event to an account pair. Don't
  compose debit/credit pairs in handlers.
- Entries are added to the change tracker and saved in the same transaction as their source
  record. They are never edited.
- `SourceType` / `SourceId` trace each entry back to its record.
- `CashInHand` / `CashPosition` enforce that the group cannot spend or lend cash it doesn't
  hold.
- `GetTrialBalance` must always balance. `TrialBalanceTests` guards this.

## Loans

- Status: `Pending → Approved → Active → PaidOff`, plus `Overdue`, `Cancelled`, `Declined`.
- Members vote (`LoanVoting`, `LoanVoteTally`, `LoanApproval`), and an admin disburses. Partial
  disbursement is supported (`CompleteDisbursement`). `ForceDisburseLoan` lets an admin
  override the vote.
- The borrower's role decides the rate. Non-members borrow without participating.
- Interest accrues daily. Each payment settles the interest accrued up to its date, then
  principal (`LoanPaymentAllocation`).
- **Editing a payment replays the loan.** `LoanPaymentReplay` rewinds the loan to disbursement
  and re-applies every remaining payment in order. A correction that would restate a payment
  already posted to the ledger is refused.

## Deposits

- `DepositType.MonthlyDeposit` needs a **Bikram Sambat** month and year (`BikramSambat.cs`,
  with years validated between 2071 and 2100). Other types have no period.
- Loan interest and repayments are recorded against the loan, not as deposits
  (`DepositType.CanBeRecorded()`).
- Late joiners pay catch-up interest as an **OtherIncomingFund**. This counts as income, not
  savings.

## Email notifications

- Emails are sent in the **group's** name (From line, body, sign-off). The one exception is
  password reset, which uses the product's name because the account spans groups.
- Admins are told when something needs verifying. The group is told once it is verified. The
  person who acted is left out of the recipients.
- Password reset always answers the same way, so nobody can use it to find out which
  addresses have accounts. Only active accounts get a link, and there is a 2-minute cooldown
  per address.
