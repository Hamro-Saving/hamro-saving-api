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

        var result = loan.Revise(50_000m, 12m, null, null);

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
        loan.Revise(500_000m, 18m, null, null);

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
        loan.Revise(100_000m, 18m, Start.AddMonths(6), "corrected reference");

        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal("corrected reference", loan.Notes);
    }

    [Fact]
    public void EditsAreRefusedOnceTheMoneyHasLeft()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        var result = loan.Revise(50_000m, 12m, null, null);

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
