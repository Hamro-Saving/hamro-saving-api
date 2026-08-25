using HamroSavings.Domain.Finance;
using HamroSavings.SharedKernel;

namespace UnitTests.Finance;

/// <summary>
/// Money in that is neither savings nor a loan repayment. The category is deliberately broad,
/// so the remark is what identifies a row later — which is why it is required rather than a
/// convenience field.
/// </summary>
public class OtherIncomingFundTests
{
    private static Result<OtherIncomingFund> Record(decimal amount = 12_000m, string remarks = "Late joiner interest")
        => OtherIncomingFund.Record(Guid.NewGuid(), Guid.NewGuid(), amount, DateTime.UtcNow, remarks, Guid.NewGuid());

    [Fact]
    public void RecordingIncomeKeepsWhatItWasFor()
    {
        var result = Record(remarks: "Fine for late deposit");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fine for late deposit", result.Value.Remarks);
        Assert.Equal(12_000m, result.Value.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IncomeWithoutARemarkIsRefused(string remarks)
    {
        var result = Record(remarks: remarks);

        Assert.True(result.IsFailure);
        Assert.Equal(OtherIncomingFundErrors.RemarksRequired, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IncomeMustBeMoreThanNothing(decimal amount)
    {
        var result = Record(amount);

        Assert.True(result.IsFailure);
        Assert.Equal(OtherIncomingFundErrors.AmountNotPositive, result.Error);
    }

    [Fact]
    public void SurroundingSpaceIsNotARemark()
    {
        var result = Record(remarks: "  Refund from vendor  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Refund from vendor", result.Value.Remarks);
    }

    [Fact]
    public void AnUpdateCannotStripTheRemark()
    {
        var fund = Record().Value;

        var result = fund.Update(15_000m, DateTime.UtcNow, "   ");

        Assert.True(result.IsFailure);
        Assert.Equal(OtherIncomingFundErrors.RemarksRequired, result.Error);
        Assert.Equal("Late joiner interest", fund.Remarks);
        Assert.Equal(12_000m, fund.Amount);
    }
}
