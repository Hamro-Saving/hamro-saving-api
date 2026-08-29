using HamroSavings.Domain.Finance;

namespace UnitTests.Finance;

/// <summary>
/// Correcting or removing a record while it is unverified. The rule is the same everywhere:
/// up to verification a record is only a claim about money, so it can be fixed; after it, the
/// money has moved in the group's books and a correction is an opposite entry instead.
/// </summary>
public class UnverifiedCorrectionTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Maturity = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static FixedDeposit Deposit() =>
        FixedDeposit.Create(Guid.NewGuid(), "Nabil Bank", 200_000m, 9m, Start, Maturity, null, Guid.NewGuid());

    /// <summary>Placed, checked, and since withdrawn — but the withdrawal is not verified yet.</summary>
    private static FixedDeposit WithdrawnDeposit()
    {
        var fd = Deposit();
        fd.Verify(Guid.NewGuid());
        fd.Withdraw(9_000m, Maturity, Guid.NewGuid());
        return fd;
    }

    private static Expense Expense() =>
        HamroSavings.Domain.Finance.Expense.Create(Guid.NewGuid(), 5_000m, "Administrative", "Stationery", Start, Guid.NewGuid());

    private static OtherIncomingFund IncomingFund() =>
        OtherIncomingFund.Record(Guid.NewGuid(), Guid.NewGuid(), 12_000m, Start, "Late joiner interest", Guid.NewGuid()).Value;

    // --- Placements

    [Fact]
    public void AnUnverifiedPlacement_CanBeCorrected()
    {
        var fd = Deposit();

        var result = fd.Update("Nepal Bank", 250_000m, 10m, Start, Maturity, "moved");

        Assert.True(result.IsSuccess);
        Assert.Equal("Nepal Bank", fd.InstitutionName);
        Assert.Equal(250_000m, fd.Amount);
        Assert.Equal(275_000m, fd.ExpectedMaturityAmount);
    }

    [Fact]
    public void AVerifiedPlacement_CannotBeCorrected()
    {
        var fd = Deposit();
        fd.Verify(Guid.NewGuid());

        var result = fd.Update("Nepal Bank", 250_000m, 10m, Start, Maturity, null);

        Assert.True(result.IsFailure);
        Assert.Equal(FixedDepositErrors.CannotModifyVerified, result.Error);
    }

    // --- Withdrawals, which are checked separately from the placement they close

    [Fact]
    public void AnUnverifiedWithdrawal_CanBeRestated()
    {
        var fd = WithdrawnDeposit();

        var result = fd.ReviseWithdrawal(8_250.75m, Maturity.AddDays(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(8_250.75m, fd.InterestEarned);
        Assert.Equal(Maturity.AddDays(3), fd.WithdrawnAt);
        Assert.Equal(FixedDepositStatus.Withdrawn, fd.Status);
    }

    [Fact]
    public void RestatingAWithdrawal_KeepsTheSameRulesAsMakingOne()
    {
        var fd = WithdrawnDeposit();

        Assert.Equal(FixedDepositErrors.NegativeInterest, fd.ReviseWithdrawal(-1m, Maturity).Error);
        Assert.Equal(FixedDepositErrors.WithdrawnBeforeStart, fd.ReviseWithdrawal(0m, Start.AddDays(-1)).Error);
    }

    [Fact]
    public void AVerifiedWithdrawal_CannotBeRestatedOrTakenBack()
    {
        var fd = WithdrawnDeposit();
        fd.VerifyWithdrawal(Guid.NewGuid());

        Assert.Equal(FixedDepositErrors.WithdrawalAlreadyVerified, fd.ReviseWithdrawal(1m, Maturity).Error);
        Assert.Equal(FixedDepositErrors.WithdrawalAlreadyVerified, fd.CancelWithdrawal().Error);
    }

    [Fact]
    public void TakingBackAWithdrawal_LeavesTheDepositPlacedAsItWas()
    {
        var fd = WithdrawnDeposit();

        var result = fd.CancelWithdrawal();

        Assert.True(result.IsSuccess);
        Assert.Equal(FixedDepositStatus.Active, fd.Status);
        Assert.Null(fd.InterestEarned);
        Assert.Null(fd.WithdrawnAt);
        Assert.False(fd.IsWithdrawalVerified);
        // The placement itself was never in question
        Assert.True(fd.IsVerified);
        // And it can be withdrawn again, properly this time
        Assert.True(fd.Withdraw(9_500m, Maturity, Guid.NewGuid()).IsSuccess);
    }

    [Fact]
    public void ADepositThatWasNeverWithdrawn_HasNoWithdrawalToCorrect()
    {
        var fd = Deposit();
        fd.Verify(Guid.NewGuid());

        Assert.Equal(FixedDepositErrors.NotWithdrawn, fd.ReviseWithdrawal(1m, Maturity).Error);
        Assert.Equal(FixedDepositErrors.NotWithdrawn, fd.CancelWithdrawal().Error);
    }

    // --- Expenses and incoming funds

    [Fact]
    public void AnUnverifiedExpense_CanBeCorrected()
    {
        var expense = Expense();

        var result = expense.Update(6_500m, "Event", "Annual meeting", Start.AddDays(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(6_500m, expense.Amount);
        Assert.Equal("Event", expense.Category);
    }

    [Fact]
    public void AVerifiedExpense_CannotBeCorrected()
    {
        var expense = Expense();
        expense.Verify(Guid.NewGuid());

        var result = expense.Update(6_500m, "Event", "Annual meeting", Start);

        Assert.True(result.IsFailure);
        Assert.Equal(ExpenseErrors.CannotModifyVerified, result.Error);
    }

    [Fact]
    public void AVerifiedReceipt_CannotBeCorrected()
    {
        var fund = IncomingFund();
        fund.Verify(Guid.NewGuid());

        var result = fund.Update(15_000m, Start, "Fine");

        Assert.True(result.IsFailure);
        Assert.Equal(OtherIncomingFundErrors.CannotModifyVerified, result.Error);
    }
}
