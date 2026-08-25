using HamroSavings.Domain.Savings;

namespace UnitTests.Savings;

/// <summary>
/// Money coming back from a loan is not a deposit. A deposit credits member savings — what
/// the group owes its members — while a repayment credits the receivable and interest income.
/// Recording one as the other would leave the books claiming the group owes the money back.
/// </summary>
public class DepositTypeTests
{
    [Theory]
    [InlineData(DepositType.MonthlyDeposit)]
    [InlineData(DepositType.Other)]
    public void SavingsCanBeRecordedAsADeposit(DepositType type)
    {
        Assert.True(type.CanBeRecorded());
    }

    [Theory]
    [InlineData(DepositType.InterestPayment)]
    [InlineData(DepositType.LoanRepayment)]
    public void LoanMoneyCannotBeRecordedAsADeposit(DepositType type)
    {
        Assert.False(type.CanBeRecorded());
    }

    [Fact]
    public void TheRetiredTypesStillExistSoOlderDepositsKeepTheirMeaning()
    {
        // They are refused on the way in, not removed: rows already carrying them must
        // still parse and display.
        Assert.Equal(4, Enum.GetValues<DepositType>().Length);
    }
}
