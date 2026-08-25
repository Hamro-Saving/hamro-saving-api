using HamroSavings.Domain.Savings;

namespace UnitTests.Savings;

/// <summary>
/// A deposit can be corrected until it is verified — someone recording a batch at the end of
/// the month gets the amount, the remark or the date wrong, and fixing it must not require
/// deleting and re-entering. Once verified it is in the books and is settled.
/// </summary>
public class DepositUpdateTests
{
    private static Deposit UnverifiedDeposit(DateOnly? on = null) =>
        Deposit.Create(
            memberId: Guid.NewGuid(),
            groupId: Guid.NewGuid(),
            amount: 8_000,
            month: 4,
            year: 2082,
            depositDate: on ?? Today,
            type: DepositType.MonthlyDeposit,
            notes: null,
            createdById: Guid.NewGuid());

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void CorrectingADepositMovesItsDate()
    {
        var deposit = UnverifiedDeposit();
        var backdated = Today.AddDays(-10);

        var result = deposit.Update(12_000, "paid late", backdated);

        Assert.True(result.IsSuccess);
        Assert.Equal(12_000, deposit.Amount);
        Assert.Equal("paid late", deposit.Notes);
        Assert.Equal(backdated, deposit.DepositDate);
    }

    [Fact]
    public void ADepositCannotBeDatedIntoTheFuture()
    {
        var deposit = UnverifiedDeposit();
        var original = deposit.DepositDate;

        var result = deposit.Update(8_000, null, Today.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(DepositErrors.DepositDateInFuture, result.Error);
        Assert.Equal(original, deposit.DepositDate);
    }

    [Fact]
    public void TodayIsStillAllowed()
    {
        var deposit = UnverifiedDeposit(Today.AddDays(-3));

        Assert.True(deposit.Update(8_000, null, Today).IsSuccess);
        Assert.Equal(Today, deposit.DepositDate);
    }

    [Fact]
    public void AVerifiedDepositKeepsItsDate()
    {
        var deposit = UnverifiedDeposit();
        var original = deposit.DepositDate;
        deposit.Verify(Guid.NewGuid());

        var result = deposit.Update(8_000, null, Today.AddDays(-1));

        Assert.True(result.IsFailure);
        Assert.Equal(DepositErrors.CannotModifyVerified, result.Error);
        Assert.Equal(original, deposit.DepositDate);
    }
}
