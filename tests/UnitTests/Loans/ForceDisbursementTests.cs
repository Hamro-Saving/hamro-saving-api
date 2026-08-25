using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;

namespace UnitTests.Loans;

/// <summary>
/// An admin can pay out a loan the members never answered. The escape is from silence, not
/// from a refusal: a group that voted the loan down keeps its answer, and every other rule
/// that guards a disbursement still applies.
/// </summary>
public class ForceDisbursementTests
{
    private static readonly CashInHand Funded = new(10_000_000m);
    private static readonly DateTime Start = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Six voters: four declines settle it, three leave it open.</summary>
    private static LoanVoteTally Votes(int declines) => new(declines, DeclinesNeeded: 4);

    private static Loan NewLoan() =>
        Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), 100_000m, 18m, Start, null, null);

    [Fact]
    public void ALoanNobodyVotedOnCanBeForcedThrough()
    {
        var loan = NewLoan();
        var admin = Guid.NewGuid();

        var result = loan.ForceDisbursement(admin, Start, Funded, Votes(0));

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(admin, loan.DisbursedById);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
        Assert.True(loan.IsForceDisbursed);
    }

    [Fact]
    public void SomeDeclinesShortOfHalfDoNotBlockIt()
    {
        var loan = NewLoan();

        Assert.True(loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(3)).IsSuccess);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void HalfTheGroupDecliningStopsIt()
    {
        var loan = NewLoan();

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(4));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.GroupRefusedLoan, result.Error);
        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.False(loan.IsForceDisbursed);
    }

    [Fact]
    public void MoreThanHalfDecliningStopsIt()
    {
        var loan = NewLoan();

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(6));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.GroupRefusedLoan, result.Error);
    }

    [Fact]
    public void ADeclinedLoanCannotBeRevived()
    {
        var loan = NewLoan();
        loan.Decline();

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(0));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.CannotForceDisburse, result.Error);
        Assert.Equal(LoanStatus.Declined, loan.Status);
    }

    [Fact]
    public void ACancelledLoanCannotBeRevived()
    {
        var loan = NewLoan();
        loan.Cancel();

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(0));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.CannotForceDisburse, result.Error);
        Assert.Equal(LoanStatus.Cancelled, loan.Status);
    }

    [Fact]
    public void ForcingCannotSpendMoneyTheGroupDoesNotHave()
    {
        var loan = NewLoan();

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, new CashInHand(99_999m), Votes(0));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.False(loan.IsForceDisbursed);
    }

    [Fact]
    public void AnAlreadyApprovedLoanIsNotMarkedAsForced()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        Assert.True(loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(0)).IsSuccess);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.False(loan.IsForceDisbursed);
    }

    [Theory]
    [InlineData(4, 3, false)]   // six voters need four declines; three leaves it open
    [InlineData(4, 4, true)]    // an even group splitting evenly settles as refused
    [InlineData(4, 6, true)]
    [InlineData(3, 3, true)]    // five voters need three
    [InlineData(0, 0, false)]   // nobody eligible to vote is not everybody refusing
    public void ARefusalNeedsHalfTheGroup(int declinesNeeded, int declines, bool refused)
    {
        Assert.Equal(refused, new LoanVoteTally(declines, declinesNeeded).GroupHasRefused);
    }

    [Fact]
    public void APayoutCanBeBackdatedSoInterestRunsFromWhenTheMoneyLeft()
    {
        var loan = NewLoan();
        var handedOverOn = Start.AddDays(-400);

        Assert.True(loan.ForceDisbursement(Guid.NewGuid(), handedOverOn, Funded, Votes(0)).IsSuccess);

        Assert.Equal(handedOverOn, loan.DisbursedAt);
        Assert.Equal(handedOverOn, loan.LastAccrualDate);
        Assert.Equal(400, loan.AccrualDays(Start));
    }

    [Fact]
    public void APayoutCannotBeDatedIntoTheFuture()
    {
        var loan = NewLoan();

        var result = loan.ForceDisbursement(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), Funded, Votes(0));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.DisbursementInFuture, result.Error);
        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.False(loan.IsForceDisbursed);
    }

    [Fact]
    public void AnActiveLoanCannotBeDisbursedTwice()
    {
        var loan = NewLoan();
        loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(0));

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, Votes(0));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.CannotForceDisburse, result.Error);
    }
}
