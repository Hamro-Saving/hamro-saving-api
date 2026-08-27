using HamroSavings.Domain.Finance;

namespace UnitTests.Finance;

/// <summary>
/// Verification is the gate onto the books, and for an expense it is the only check there is —
/// nothing votes on spending. So it has to be real: unverified means not posted, verified
/// means settled and no longer editable.
/// </summary>
public class FinanceVerificationTests
{
    private static readonly Guid Group = Guid.NewGuid();
    private static readonly DateTime Today = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Expense AnExpense() =>
        Expense.Create(Group, 5_000m, "Stationery", "Ledger books", Today, Guid.NewGuid());

    private static FixedDeposit AFixedDeposit() =>
        FixedDeposit.Create(Group, "Nabil Bank", 200_000m, 9m, Today, Today.AddYears(1), null, Guid.NewGuid());

    private static OtherIncomingFund AnIncomingFund() =>
        OtherIncomingFund.Record(Group, Guid.NewGuid(), 3_000m, Today, "Late joiner interest", Guid.NewGuid()).Value;

    // ---------- Expenses ----------

    [Fact]
    public void AnExpenseStartsUnverified()
    {
        var expense = AnExpense();

        Assert.False(expense.IsVerified);
        Assert.Null(expense.VerifiedById);
        Assert.Single(expense.DomainEvents.OfType<ExpenseRecordedDomainEvent>());
    }

    [Fact]
    public void VerifyingAnExpenseRecordsWhoAndWhen()
    {
        var expense = AnExpense();
        var admin = Guid.NewGuid();
        expense.ClearDomainEvents();

        Assert.True(expense.Verify(admin).IsSuccess);
        Assert.True(expense.IsVerified);
        Assert.Equal(admin, expense.VerifiedById);
        Assert.NotNull(expense.VerifiedAt);
        Assert.Single(expense.DomainEvents.OfType<ExpenseVerifiedDomainEvent>());
    }

    [Fact]
    public void AnExpenseCannotBeVerifiedTwice()
    {
        var expense = AnExpense();
        expense.Verify(Guid.NewGuid());
        expense.ClearDomainEvents();

        var again = expense.Verify(Guid.NewGuid());

        // Verification is what posts to the ledger, so a second one would spend the money twice.
        Assert.True(again.IsFailure);
        Assert.Equal(ExpenseErrors.AlreadyVerified, again.Error);
        Assert.Empty(expense.DomainEvents);
    }

    [Fact]
    public void AVerifiedExpenseCanNoLongerBeCorrected()
    {
        var expense = AnExpense();
        Assert.True(expense.Update(6_000m, "Stationery", "Corrected", Today).IsSuccess);

        expense.Verify(Guid.NewGuid());

        var result = expense.Update(9_999m, "Other", "Changed after the fact", Today);
        Assert.True(result.IsFailure);
        Assert.Equal(ExpenseErrors.CannotModifyVerified, result.Error);
        Assert.Equal(6_000m, expense.Amount);
    }

    // ---------- Fixed deposits ----------

    [Fact]
    public void AFixedDepositCannotBeWithdrawnBeforeItsPlacementIsVerified()
    {
        var fd = AFixedDeposit();

        // The placement is not on the books yet, so a return posted against it would credit
        // the group money it never recorded going out.
        var result = fd.Withdraw(18_000m, Today.AddYears(1), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(FixedDepositErrors.NotVerified, result.Error);
        Assert.Equal(FixedDepositStatus.Active, fd.Status);
    }

    [Fact]
    public void ThePlacementAndTheWithdrawalAreVerifiedSeparately()
    {
        var fd = AFixedDeposit();
        fd.Verify(Guid.NewGuid());

        Assert.True(fd.IsVerified);
        // Verifying the placement says nothing about a withdrawal that has not happened.
        Assert.False(fd.IsWithdrawalVerified);

        Assert.True(fd.Withdraw(18_000m, Today.AddYears(1), Guid.NewGuid()).IsSuccess);
        Assert.Equal(FixedDepositStatus.Withdrawn, fd.Status);
        Assert.False(fd.IsWithdrawalVerified);
        Assert.Single(fd.DomainEvents.OfType<FixedDepositWithdrawalRecordedDomainEvent>());

        var admin = Guid.NewGuid();
        Assert.True(fd.VerifyWithdrawal(admin).IsSuccess);
        Assert.True(fd.IsWithdrawalVerified);
        Assert.Equal(admin, fd.WithdrawalVerifiedById);
    }

    [Fact]
    public void ThereIsNoWithdrawalToVerifyUntilOneHappens()
    {
        var fd = AFixedDeposit();
        fd.Verify(Guid.NewGuid());

        var result = fd.VerifyWithdrawal(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(FixedDepositErrors.NotWithdrawn, result.Error);
    }

    [Fact]
    public void AWithdrawalCannotBeVerifiedTwice()
    {
        var fd = AFixedDeposit();
        fd.Verify(Guid.NewGuid());
        fd.Withdraw(18_000m, Today.AddYears(1), Guid.NewGuid());
        fd.VerifyWithdrawal(Guid.NewGuid());

        var again = fd.VerifyWithdrawal(Guid.NewGuid());

        Assert.True(again.IsFailure);
        Assert.Equal(FixedDepositErrors.WithdrawalAlreadyVerified, again.Error);
    }

    // ---------- Other incoming funds ----------

    [Fact]
    public void AnIncomingFundStartsUnverifiedAndCanBeVerifiedOnce()
    {
        var record = AnIncomingFund();
        Assert.False(record.IsVerified);
        Assert.Single(record.DomainEvents.OfType<OtherIncomingFundRecordedDomainEvent>());

        record.ClearDomainEvents();
        Assert.True(record.Verify(Guid.NewGuid()).IsSuccess);
        Assert.True(record.IsVerified);
        Assert.Single(record.DomainEvents.OfType<OtherIncomingFundVerifiedDomainEvent>());

        var again = record.Verify(Guid.NewGuid());
        Assert.True(again.IsFailure);
        Assert.Equal(OtherIncomingFundErrors.AlreadyVerified, again.Error);
    }

    [Fact]
    public void AVerifiedIncomingFundCanNoLongerBeCorrected()
    {
        var record = AnIncomingFund();
        Assert.True(record.Update(4_000m, Today, "Corrected remark").IsSuccess);

        record.Verify(Guid.NewGuid());

        var result = record.Update(9_999m, Today, "Changed after the fact");
        Assert.True(result.IsFailure);
        Assert.Equal(OtherIncomingFundErrors.CannotModifyVerified, result.Error);
        Assert.Equal(4_000m, record.Amount);
    }
}
