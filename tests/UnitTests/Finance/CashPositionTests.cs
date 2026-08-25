namespace UnitTests.Finance;

/// <summary>
/// A group can only commit money it is actually holding. The limit is cash in hand, which
/// is what the ledger says is left after everything already deposited, lent, spent and
/// parked — not the total ever collected.
/// </summary>
public class CashPositionTests
{
    private static decimal InHand(decimal inflow, decimal outflow) => inflow - outflow;

    [Theory]
    [InlineData(100_000, 40_000, 60_000)]
    [InlineData(100_000, 100_000, 0)]
    [InlineData(50_000, 80_000, -30_000)]
    public void CashInHandIsWhatCameInLessWhatWentOut(decimal inflow, decimal outflow, decimal expected)
    {
        Assert.Equal(expected, InHand(inflow, outflow));
    }

    [Theory]
    [InlineData(60_000, 60_000, true)]   // exactly the balance is allowed
    [InlineData(60_000, 59_999, true)]
    [InlineData(60_000, 60_001, false)]
    public void SpendingIsAllowedUpToTheBalanceAndNoFurther(decimal inHand, decimal requested, bool allowed)
    {
        Assert.Equal(allowed, requested <= inHand);
    }

    [Fact]
    public void NothingCanBeCommittedWhileTheGroupIsOverdrawn()
    {
        var inHand = InHand(50_000, 80_000);

        Assert.True(inHand < 0);
        Assert.False(1m <= inHand);
    }
}
