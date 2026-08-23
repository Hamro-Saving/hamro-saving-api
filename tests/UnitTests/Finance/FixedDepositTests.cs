using HamroSavings.Domain.Finance;

namespace UnitTests.Finance;

public class FixedDepositTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Maturity = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static FixedDeposit Deposit() =>
        FixedDeposit.Create(Guid.NewGuid(), "Nabil Bank", 200_000m, 9m, Start, Maturity, null, Guid.NewGuid());

    [Fact]
    public void NewDeposit_IsActive()
    {
        var fd = Deposit();

        Assert.Equal(FixedDepositStatus.Active, fd.Status);
        Assert.Equal(FixedDepositStatus.Active, fd.StatusAsOf(Start));
        Assert.False(fd.HasMatured(Start));
    }

    [Fact]
    public void Deposit_StaysActiveUpToTheDayBeforeMaturity()
    {
        var fd = Deposit();

        Assert.Equal(FixedDepositStatus.Active, fd.StatusAsOf(Maturity.AddDays(-1)));
    }

    [Fact]
    public void Deposit_ReadsAsMaturedOnTheMaturityDate()
    {
        var fd = Deposit();

        Assert.True(fd.HasMatured(Maturity));
        Assert.Equal(FixedDepositStatus.Matured, fd.StatusAsOf(Maturity));
        Assert.Equal(FixedDepositStatus.Matured, fd.StatusAsOf(Maturity.AddYears(1)));
    }

    [Fact]
    public void MaturityIsJudgedByDate_NotTimeOfDay()
    {
        var fd = Deposit();

        // Any moment on the maturity day counts, whatever the clock says
        Assert.Equal(FixedDepositStatus.Matured, fd.StatusAsOf(Maturity.AddHours(1)));
        Assert.Equal(FixedDepositStatus.Active, fd.StatusAsOf(Maturity.AddHours(-1)));
    }

    [Fact]
    public void WithdrawnDeposit_StaysWithdrawnAfterMaturity()
    {
        var fd = Deposit();
        fd.Withdraw(18_000m, Maturity, Guid.NewGuid());

        Assert.Equal(FixedDepositStatus.Withdrawn, fd.StatusAsOf(Maturity.AddDays(30)));
    }

    [Fact]
    public void Withdrawal_RecordsTheInterestActuallyReturned()
    {
        var fd = Deposit();
        var admin = Guid.NewGuid();
        var on = Maturity.AddDays(3);

        // The institution paid less than the expected 18,000
        var result = fd.Withdraw(17_250.50m, on, admin);

        Assert.True(result.IsSuccess);
        Assert.Equal(FixedDepositStatus.Withdrawn, fd.Status);
        Assert.Equal(17_250.50m, fd.InterestEarned);
        Assert.Equal(on, fd.WithdrawnAt);
        Assert.Equal(admin, fd.WithdrawnById);
    }

    [Fact]
    public void EarlyWithdrawal_IsAllowedAndKeepsItsOwnInterestFigure()
    {
        var fd = Deposit();

        var result = fd.Withdraw(4_000m, Maturity.AddMonths(-2), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(4_000m, fd.InterestEarned);
        Assert.NotEqual(fd.ExpectedMaturityAmount - 200_000m, fd.InterestEarned);
    }

    [Fact]
    public void Withdrawal_IsRefused_WhenAlreadyWithdrawn()
    {
        var fd = Deposit();
        fd.Withdraw(18_000m, Maturity, Guid.NewGuid());

        var result = fd.Withdraw(500m, Maturity.AddDays(1), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("FixedDeposit.AlreadyWithdrawn", result.Error.Code);
        Assert.Equal(18_000m, fd.InterestEarned);
    }

    [Fact]
    public void Withdrawal_IsRefused_WhenInterestIsNegative()
    {
        var fd = Deposit();

        var result = fd.Withdraw(-1m, Maturity, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("FixedDeposit.NegativeInterest", result.Error.Code);
        Assert.Equal(FixedDepositStatus.Active, fd.Status);
    }

    [Fact]
    public void Withdrawal_IsRefused_WhenDatedBeforeTheDepositStarted()
    {
        var fd = Deposit();

        var result = fd.Withdraw(0m, Start.AddDays(-1), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("FixedDeposit.WithdrawnBeforeStart", result.Error.Code);
    }

    [Fact]
    public void ZeroInterest_IsAcceptable()
    {
        var fd = Deposit();

        Assert.True(fd.Withdraw(0m, Maturity, Guid.NewGuid()).IsSuccess);
        Assert.Equal(0m, fd.InterestEarned);
    }

    [Fact]
    public void ExpectedMaturityAmount_AddsTheFullRateToThePrincipal()
    {
        var fd = Deposit();

        Assert.Equal(218_000m, fd.ExpectedMaturityAmount);
    }
}
