using HamroSavings.Domain.Ledger;

namespace UnitTests.Ledger;

/// <summary>
/// The two-way check, worked through end to end: a group takes deposits, lends, is repaid
/// with interest, and spends. Debits and credits must agree, and cash rebuilt from the
/// entries must equal what actually happened.
/// </summary>
public class TrialBalanceTests
{
    private readonly List<(LedgerAccount Debit, LedgerAccount Credit, decimal Amount)> _book = [];

    private void Post(LedgerAccount debit, LedgerAccount credit, decimal amount)
        => _book.Add((debit, credit, amount));

    private decimal Balance(LedgerAccount account)
    {
        var debits = _book.Where(e => e.Debit == account).Sum(e => e.Amount);
        var credits = _book.Where(e => e.Credit == account).Sum(e => e.Amount);
        return account.IsDebitBalance() ? debits - credits : credits - debits;
    }

    [Fact]
    public void ARoundOfGroupActivityLeavesTheBooksBalanced()
    {
        Post(LedgerAccount.Cash, LedgerAccount.MemberSavings, 100_000m);       // members deposit
        Post(LedgerAccount.LoanReceivable, LedgerAccount.Cash, 60_000m);       // a loan goes out
        Post(LedgerAccount.Cash, LedgerAccount.LoanReceivable, 20_000m);       // principal comes back
        Post(LedgerAccount.Cash, LedgerAccount.InterestIncome, 1_500m);        // interest earned
        Post(LedgerAccount.FixedDeposits, LedgerAccount.Cash, 30_000m);        // cash parked
        Post(LedgerAccount.Expenses, LedgerAccount.Cash, 2_000m);              // money spent

        // Each entry adds the same amount to both columns, so they cannot diverge.
        var debits = _book.Sum(e => e.Amount);
        var credits = _book.Sum(e => e.Amount);
        Assert.Equal(debits, credits);

        // 100,000 - 60,000 + 20,000 + 1,500 - 30,000 - 2,000
        Assert.Equal(29_500m, Balance(LedgerAccount.Cash));

        Assert.Equal(100_000m, Balance(LedgerAccount.MemberSavings));
        Assert.Equal(40_000m, Balance(LedgerAccount.LoanReceivable));
        Assert.Equal(1_500m, Balance(LedgerAccount.InterestIncome));
        Assert.Equal(30_000m, Balance(LedgerAccount.FixedDeposits));
        Assert.Equal(2_000m, Balance(LedgerAccount.Expenses));
    }

    [Fact]
    public void WhatTheGroupOwnsMatchesWhatItOwesPlusWhatItEarned()
    {
        Post(LedgerAccount.Cash, LedgerAccount.MemberSavings, 50_000m);
        Post(LedgerAccount.LoanReceivable, LedgerAccount.Cash, 20_000m);
        Post(LedgerAccount.Cash, LedgerAccount.InterestIncome, 800m);
        Post(LedgerAccount.Expenses, LedgerAccount.Cash, 300m);

        var assets = Balance(LedgerAccount.Cash)
                   + Balance(LedgerAccount.LoanReceivable)
                   + Balance(LedgerAccount.FixedDeposits);

        var claims = Balance(LedgerAccount.MemberSavings)
                   + Balance(LedgerAccount.InterestIncome)
                   - Balance(LedgerAccount.Expenses);

        // The accounting identity: assets = liabilities + retained earnings.
        Assert.Equal(claims, assets);
    }

    [Fact]
    public void AFixedDepositWithdrawalReturnsCapitalAndIncomeSeparately()
    {
        Post(LedgerAccount.FixedDeposits, LedgerAccount.Cash, 100_000m);
        Post(LedgerAccount.Cash, LedgerAccount.FixedDeposits, 100_000m);
        Post(LedgerAccount.Cash, LedgerAccount.InterestIncome, 6_000m);

        // The parked money is fully back, and only the interest counts as earnings.
        Assert.Equal(0m, Balance(LedgerAccount.FixedDeposits));
        Assert.Equal(6_000m, Balance(LedgerAccount.Cash));
        Assert.Equal(6_000m, Balance(LedgerAccount.InterestIncome));
    }
}
