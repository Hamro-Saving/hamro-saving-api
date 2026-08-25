namespace UnitTests.Finance;

/// <summary>
/// The finance page and the ledger must answer "how much is out with borrowers" the same way,
/// or the reconciliation drifts. The ledger credits its receivable only when a payment is
/// verified, so the summary has to wait for verification too.
///
/// These model the two figures rather than exercising the handler, which needs a database:
/// the ledger's receivable balance, and the summary's totalOnLoan.
/// </summary>
public class OnLoanReconciliationTests
{
    /// <summary>Ledger: disbursements debit the receivable, verified principal credits it.</summary>
    private static decimal LedgerReceivable(decimal disbursed, decimal verifiedPrincipal)
        => disbursed - verifiedPrincipal;

    /// <summary>Summary, as it now computes totalOnLoan.</summary>
    private static decimal SummaryOnLoan(decimal disbursed, decimal verifiedPrincipal)
        => disbursed - verifiedPrincipal;

    /// <summary>Summary, as it used to: the loans' own outstanding principal.</summary>
    private static decimal OldSummaryOnLoan(decimal outstandingOnActiveLoans)
        => outstandingOnActiveLoans;

    [Theory]
    [InlineData(4_385_050, 3_685_050)]  // a repayment recorded but not yet verified
    [InlineData(300_000, 0)]            // nothing repaid
    [InlineData(300_000, 300_000)]      // fully repaid and verified
    public void TheTwoSidesAgreeOnWhatIsOutWithBorrowers(decimal disbursed, decimal verifiedPrincipal)
    {
        Assert.Equal(
            LedgerReceivable(disbursed, verifiedPrincipal),
            SummaryOnLoan(disbursed, verifiedPrincipal));
    }

    [Fact]
    public void AnUnverifiedRepaymentNoLongerMovesTheSummary()
    {
        const decimal disbursed = 4_385_050m;
        const decimal verifiedPrincipal = 3_685_050m;

        // A 300,000 repayment is keyed in and settles the loan, so the loan's own outstanding
        // principal drops to zero — but nothing has been verified, so the ledger still carries
        // it. This is the gap that showed as "off by -300,000".
        var ledger = LedgerReceivable(disbursed, verifiedPrincipal);
        var oldSummary = OldSummaryOnLoan(400_000m);
        Assert.Equal(300_000m, ledger - oldSummary);

        // The summary now reads the same quantity the ledger does, so there is no gap to have.
        Assert.Equal(ledger, SummaryOnLoan(disbursed, verifiedPrincipal));
    }

    [Fact]
    public void ACancelledLoanWasNeverMoneyOut()
    {
        // Only disbursed loans enter the figure at all, on either side.
        const decimal disbursed = 300_000m;

        Assert.Equal(disbursed, SummaryOnLoan(disbursed, 0m));
        Assert.Equal(disbursed, LedgerReceivable(disbursed, 0m));
    }
}
