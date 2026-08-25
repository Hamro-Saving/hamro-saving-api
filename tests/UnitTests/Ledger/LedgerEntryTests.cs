using HamroSavings.Domain.Ledger;

namespace UnitTests.Ledger;

/// <summary>
/// The ledger's guarantee is that no single entry can be malformed: every one moves a
/// positive amount between two different accounts. That is what makes the totals prove out.
/// </summary>
public class LedgerEntryTests
{
    private static readonly Guid Group = Guid.NewGuid();
    private static readonly DateTime When = new(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);

    private static bool CanPost(decimal amount, LedgerAccount debit, LedgerAccount credit) =>
        LedgerEntry.Post(Group, When, TransactionType.Deposit, debit, credit, amount,
            "test", "Deposit", Guid.NewGuid()).IsSuccess;

    [Fact]
    public void AnEntryNamesBothSidesOfTheMovement()
    {
        var entry = LedgerEntry.Post(Group, When, TransactionType.Deposit,
            LedgerAccount.Cash, LedgerAccount.MemberSavings, 5_000m,
            "Deposit verified", "Deposit", Guid.NewGuid()).Value;

        Assert.Equal(LedgerAccount.Cash, entry.DebitAccount);
        Assert.Equal(LedgerAccount.MemberSavings, entry.CreditAccount);
        Assert.Equal(5_000m, entry.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5000)]
    public void AnEntryThatMovesNothingIsRejected(decimal amount)
    {
        Assert.False(CanPost(amount, LedgerAccount.Cash, LedgerAccount.MemberSavings));
    }

    [Fact]
    public void AnEntryCannotDebitAndCreditTheSameAccount()
    {
        Assert.False(CanPost(100m, LedgerAccount.Cash, LedgerAccount.Cash));
    }

    [Theory]
    [InlineData(LedgerAccount.Cash, true)]
    [InlineData(LedgerAccount.LoanReceivable, true)]
    [InlineData(LedgerAccount.FixedDeposits, true)]
    [InlineData(LedgerAccount.Expenses, true)]
    [InlineData(LedgerAccount.MemberSavings, false)]
    [InlineData(LedgerAccount.InterestIncome, false)]
    public void AssetsAndExpensesRunOnTheDebitSide(LedgerAccount account, bool debitBalance)
    {
        Assert.Equal(debitBalance, account.IsDebitBalance());
    }
}
