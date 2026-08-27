using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;

namespace UnitTests.Loans;

/// <summary>
/// Pending → (members vote) Approved → (admin marks disbursement complete) Active,
/// with an admin cancel available up to the moment the money leaves the group.
/// </summary>
public class LoanLifecycleTests
{
    private static readonly CashInHand Funded = new(10_000_000m);

    private static readonly DateTime Start = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Loan NewLoan() =>
        Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), 100_000m, 18m, Start, null, null);

    [Fact]
    public void NewLoan_StartsPendingWithAnEmptyLedger()
    {
        var loan = NewLoan();

        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Null(loan.DisbursedAt);
        Assert.Null(loan.LastAccrualDate);
        Assert.Equal(0m, loan.OutstandingPrincipal);
    }

    [Fact]
    public void Approval_MovesAPendingLoanToApproved()
    {
        var loan = NewLoan();

        Assert.True(loan.ApproveLoan().IsSuccess);
        Assert.Equal(LoanStatus.Approved, loan.Status);
        // Approving twice is not a thing
        Assert.Equal("Loan.NotPending", loan.ApproveLoan().Error.Code);
    }

    [Fact]
    public void Decline_MovesAPendingLoanToDeclined()
    {
        var loan = NewLoan();

        Assert.True(loan.Decline().IsSuccess);
        Assert.Equal(LoanStatus.Declined, loan.Status);
    }

    [Fact]
    public void Disbursement_RequiresTheMembersToHaveApprovedFirst()
    {
        var loan = NewLoan();

        var result = loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.NotApproved", result.Error.Code);
        Assert.Equal(LoanStatus.Pending, loan.Status);
    }

    [Fact]
    public void Disbursement_ActivatesTheLoanAndStampsWhoAndWhen()
    {
        var loan = NewLoan();
        var admin = Guid.NewGuid();
        var disbursedAt = Start.AddDays(9);
        loan.ApproveLoan();

        Assert.True(loan.CompleteDisbursement(admin, disbursedAt, Funded).IsSuccess);

        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(admin, loan.DisbursedById);
        Assert.Equal(disbursedAt, loan.DisbursedAt);
        // Interest runs from disbursement, not from the loan's start date
        Assert.Equal(disbursedAt, loan.LastAccrualDate);
        Assert.Equal(0m, loan.InterestAccruedAsOf(disbursedAt));
    }

    [Fact]
    public void ALoanTheGroupMadeEarlierCanBeEnteredWithItsRealPayoutDate()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        var handedOverOn = Start.AddDays(-1_000);

        Assert.True(loan.CompleteDisbursement(Guid.NewGuid(), handedOverOn, Funded).IsSuccess);

        // The interest clock starts when the borrower got the money, so a loan brought into
        // the system years later arrives carrying every day of interest it has already run.
        Assert.Equal(handedOverOn, loan.LastAccrualDate);
        Assert.Equal(1_000, loan.AccrualDays(Start));
        Assert.True(loan.InterestAccruedAsOf(Start) > 0);
    }

    [Fact]
    public void APayoutCannotBeDatedIntoTheFuture()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        var result = loan.CompleteDisbursement(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), Funded);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.DisbursementInFuture, result.Error);
        Assert.Equal(LoanStatus.Approved, loan.Status);
        Assert.Null(loan.LastAccrualDate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancel_IsAllowedRightUpUntilDisbursement(bool approveFirst)
    {
        var loan = NewLoan();
        if (approveFirst) loan.ApproveLoan();

        Assert.True(loan.Cancel().IsSuccess);
        Assert.Equal(LoanStatus.Cancelled, loan.Status);
    }

    [Fact]
    public void Cancel_IsRefusedOnceTheMoneyHasGoneOut()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        var result = loan.Cancel();

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.CannotCancelAfterDisbursement", result.Error.Code);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void AnApprovedLoanCanStillBeRevisedBeforeTheMoneyLeaves()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        var result = loan.Revise(50_000m, 12m, Start, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(50_000m, loan.Amount);
    }

    [Fact]
    public void RevisingAnApprovedLoanSendsItBackToTheGroup()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        // The group approved 100,000. This is a different loan, so their approval
        // cannot carry over to it.
        loan.Revise(500_000m, 18m, Start, null, null);

        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal(500_000m, loan.Amount);
    }

    [Fact]
    public void EvenAnUnchangedRevisionSendsItBack()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        // Nothing about the money moved, but the loan was still edited, and there is
        // no way to know whether a voter would still agree. They look again.
        loan.Revise(100_000m, 18m, Start, Start.AddMonths(6), "corrected reference");

        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal("corrected reference", loan.Notes);
    }

    [Fact]
    public void AShortDisbursementBecomesTheLoan()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        // The group only had 60,000 to hand over against a 100,000 request. What left is
        // what is owed back, so the loan is now a 60,000 loan.
        var result = loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded, 60_000m);

        Assert.True(result.IsSuccess);
        Assert.Equal(60_000m, loan.Amount);
        Assert.Equal(60_000m, loan.OutstandingPrincipal);
        // The members carried 100,000, and the record still says so.
        Assert.Equal(100_000m, loan.RequestedAmount);
        Assert.True(loan.WasReducedAtDisbursement);
    }

    [Fact]
    public void AFullDisbursementIsNotAReduction()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        Assert.Equal(100_000m, loan.RequestedAmount);
        Assert.False(loan.WasReducedAtDisbursement);
    }

    [Fact]
    public void RevisingBeforePayoutMovesWhatWasAskedFor()
    {
        var loan = NewLoan();

        // Still a request at this point, so changing it changes what the group is being
        // asked for — this is not a reduction, and must not read as one.
        loan.Revise(70_000m, 18m, Start, null, null);

        Assert.Equal(70_000m, loan.Amount);
        Assert.Equal(70_000m, loan.RequestedAmount);
        Assert.False(loan.WasReducedAtDisbursement);
    }

    [Fact]
    public void DisbursingMoreThanWasApprovedIsRefused()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        var result = loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded, 120_000m);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.DisbursedAmountExceedsRequest", result.Error.Code);
        // The loan is untouched — still approved, still waiting, still 100,000.
        Assert.Equal(LoanStatus.Approved, loan.Status);
        Assert.Equal(100_000m, loan.Amount);
    }

    [Fact]
    public void DisbursingNothingIsRefused()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        var result = loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded, 0m);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.DisbursedAmountNotPositive", result.Error.Code);
    }

    [Fact]
    public void OmittingTheAmountDisbursesTheWholeLoan()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        Assert.True(loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded).IsSuccess);
        Assert.Equal(100_000m, loan.Amount);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
    }

    [Fact]
    public void AShortDisbursementIsCheckedAgainstCashActuallyHeld()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        // 100,000 was asked for and the group holds only 60,000 — but it is handing over
        // 60,000, so the payout goes through on the figure that actually moves.
        var result = loan.CompleteDisbursement(Guid.NewGuid(), Start, new CashInHand(60_000m), 60_000m);

        Assert.True(result.IsSuccess);
        Assert.Equal(60_000m, loan.Amount);
    }

    [Fact]
    public void RevisingMovesTheStartDate()
    {
        var loan = NewLoan();
        var postponed = Start.AddMonths(1);

        var result = loan.Revise(100_000m, 18m, postponed, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(postponed, loan.StartDate);
    }

    [Fact]
    public void EditsAreRefusedOnceTheMoneyHasLeft()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        var result = loan.Revise(50_000m, 12m, Start, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.CannotModifyAfterDisbursement", result.Error.Code);
        Assert.Equal(100_000m, loan.Amount);
    }

    [Fact]
    public void DisbursementIsRefusedWhenTheGroupCannotCoverIt()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        // Approved when the group had the money, paid out when it no longer does.
        var result = loan.CompleteDisbursement(Guid.NewGuid(), Start, new CashInHand(99_999m));

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.InsufficientCash", result.Error.Code);
        Assert.Equal(LoanStatus.Approved, loan.Status);
        Assert.Null(loan.DisbursedAt);
    }

    [Fact]
    public void DisbursementIsAllowedWhenTheBalanceExactlyCoversIt()
    {
        var loan = NewLoan();
        loan.ApproveLoan();

        Assert.True(loan.CompleteDisbursement(Guid.NewGuid(), Start, new CashInHand(100_000m)).IsSuccess);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void CancellingIsRefusedOnceTheMoneyHasLeft()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        Assert.True(loan.Cancel().IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellingIsAllowedUntilTheMoneyLeaves(bool approvedFirst)
    {
        var loan = NewLoan();
        if (approvedFirst) loan.ApproveLoan();

        Assert.True(loan.Cancel().IsSuccess);
        Assert.Equal(LoanStatus.Cancelled, loan.Status);
    }
}
