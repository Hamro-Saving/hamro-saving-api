using HamroSavings.Domain.Ledger;

namespace UnitTests.Ledger;

/// <summary>
/// The group can commit money only up to what it is holding. The rule lives on the value
/// itself, so every path that spends — an expense, a fixed deposit, a loan applied for or
/// paid out — asks the same question and gets the same answer.
/// </summary>
public class CashInHandTests
{
    [Theory]
    [InlineData(60_000, 59_999, true)]
    [InlineData(60_000, 60_000, true)]   // exactly the balance is fine
    [InlineData(60_000, 60_001, false)]
    [InlineData(0, 1, false)]
    [InlineData(0, 0, true)]
    public void SpendingReachesTheBalanceAndStops(decimal held, decimal requested, bool allowed)
    {
        Assert.Equal(allowed, new CashInHand(held).Covers(requested));
    }

    [Fact]
    public void AnOverdrawnGroupCanCommitNothing()
    {
        var overdrawn = new CashInHand(-2_852_636.71m);

        Assert.False(overdrawn.Covers(1m));
        Assert.True(overdrawn.EnsureCovers(1m).IsFailure);
    }

    [Fact]
    public void TheRefusalSaysWhatIsHeldAndWhatWasAsked()
    {
        var result = new CashInHand(50_000m).EnsureCovers(75_000m);

        Assert.Equal("Ledger.InsufficientCash", result.Error.Code);
        Assert.Contains("50,000", result.Error.Description);
        Assert.Contains("75,000", result.Error.Description);
    }
}
