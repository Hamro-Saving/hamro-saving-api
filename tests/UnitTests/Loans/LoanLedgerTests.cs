using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;

namespace UnitTests.Loans;

/// <summary>
/// Interest runs daily on the outstanding principal at the annual rate over 365 days. It is
/// only written down when a payment settles it; between payments it is computed on the fly.
/// </summary>
public class LoanLedgerTests
{
    private static readonly CashInHand Funded = new(10_000_000m);

    private static readonly DateTime Disbursed = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>A live loan of 100,000 at 18% a year — 49.315068 a day — disbursed 2026-06-01.</summary>
    private static Loan LiveLoan(decimal amount = 100_000m, decimal rate = 18m)
    {
        var loan = Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), amount, rate, Disbursed, null, null);
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Disbursed, Funded);
        return loan;
    }

    private static Loan PendingLoan() =>
        Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), 100_000m, 18m, Disbursed, null, null);

    [Fact]
    public void DailyInterest_IsTheAnnualRateSpreadOver365Days()
    {
        var loan = LiveLoan();

        Assert.Equal(49.315068m, Math.Round(loan.DailyInterest, 6));
    }

    [Fact]
    public void Disbursement_SeedsTheLedger()
    {
        var loan = LiveLoan();

        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
        Assert.Equal(0m, loan.UnpaidInterest);
        Assert.Equal(Disbursed, loan.LastAccrualDate);
        Assert.Equal(0m, loan.InterestAccruedAsOf(Disbursed));
    }

    [Fact]
    public void InterestAccrues_PerDayOnTheOutstandingPrincipal()
    {
        var loan = LiveLoan();

        // 30 days × 49.315068 = 1,479.45
        Assert.Equal(1_479.45m, loan.InterestAccruedAsOf(Disbursed.AddDays(30)));
        Assert.Equal(101_479.45m, loan.PayoffAmountAsOf(Disbursed.AddDays(30)));
    }

    [Fact]
    public void UndisbursedLoan_AccruesNothing()
    {
        var loan = PendingLoan();

        Assert.Equal(0m, loan.InterestAccruedAsOf(Disbursed.AddDays(30)));
        Assert.Equal(0m, loan.OutstandingPrincipal);
    }

    [Fact]
    public void InterestOnlyPayment_ClearsInterestAndLeavesPrincipalUntouched()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        var result = loan.RecordPayment(paidOn, principalAmount: 0m, interestAmount: 1_479.45m);

        Assert.True(result.IsSuccess);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
        Assert.Equal(0m, loan.UnpaidInterest);
        Assert.Equal(1_479.45m, loan.TotalInterestPaid);
        Assert.Equal(0m, loan.InterestAccruedAsOf(paidOn));
    }

    [Fact]
    public void PartialInterestPayment_CarriesTheRemainderForward()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        loan.RecordPayment(paidOn, principalAmount: 0m, interestAmount: 1_000m);

        Assert.Equal(479.45m, loan.UnpaidInterest);
        Assert.Equal(479.45m, loan.InterestAccruedAsOf(paidOn));
    }

    [Fact]
    public void CarriedInterest_DoesNotCompound()
    {
        var loan = LiveLoan();
        var firstPayment = Disbursed.AddDays(30);

        // Leaves 479.45 of interest unpaid, principal untouched
        loan.RecordPayment(firstPayment, principalAmount: 0m, interestAmount: 1_000m);

        // The next 30 days must accrue on the 100,000 principal only, not on principal + carried interest
        var expected = 479.45m + Math.Round(loan.DailyInterest * 30, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, loan.InterestAccruedAsOf(firstPayment.AddDays(30)));
        Assert.Equal(1_958.90m, expected);
    }

    [Fact]
    public void PrincipalPayment_LowersTheDailyInterestFromThereOn()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        loan.RecordPayment(paidOn, principalAmount: 20_000m, interestAmount: 1_479.45m);

        Assert.Equal(80_000m, loan.OutstandingPrincipal);
        Assert.Equal(20_000m, loan.TotalPrincipalPaid);
        // 80,000 at 18% is 39.452055 a day
        Assert.Equal(39.452055m, Math.Round(loan.DailyInterest, 6));
        Assert.Equal(868m, Math.Round(loan.InterestAccruedAsOf(paidOn.AddDays(22)), 0));
    }

    [Fact]
    public void EachPayment_RestartsTheAccrualFromItsOwnDate()
    {
        var loan = LiveLoan();

        loan.RecordPayment(Disbursed.AddDays(30), 0m, 1_479.45m);
        var second = loan.RecordPayment(Disbursed.AddDays(60), 0m, 1_479.45m);

        // 30 days since the previous payment, not 60 since disbursement
        Assert.Equal(30, second.Value.DaysAccrued);
        Assert.Equal(1_479.45m, second.Value.InterestOwedBefore);
    }

    [Fact]
    public void Payment_RecordsTheCalculationItSettled()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(31);

        var allocation = loan.RecordPayment(paidOn, principalAmount: 20_000m, interestAmount: 1_000m).Value;

        Assert.Equal(1_528.77m, allocation.InterestOwedBefore);   // 31 × 49.315068
        Assert.Equal(31, allocation.DaysAccrued);
        Assert.Equal(1_000m, allocation.InterestPaid);
        Assert.Equal(20_000m, allocation.PrincipalPaid);
        Assert.Equal(80_000m, allocation.OutstandingPrincipalAfter);
        Assert.Equal(528.77m, allocation.UnpaidInterestAfter);
    }

    [Fact]
    public void SettlingInFull_PaysOffTheLoanAndStopsTheClock()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(45);
        var payoff = loan.PayoffAmountAsOf(paidOn);

        var result = loan.RecordPayment(paidOn, loan.OutstandingPrincipal, loan.InterestAccruedAsOf(paidOn));

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.PaidOff, loan.Status);
        Assert.Equal(0m, loan.OutstandingPrincipal);
        Assert.Equal(0m, loan.UnpaidInterest);
        Assert.Equal(102_219.18m, payoff);
        // A closed loan keeps accruing nothing, however long it sits
        Assert.Equal(0m, loan.InterestAccruedAsOf(paidOn.AddDays(365)));
    }

    [Fact]
    public void Totals_AccumulateAcrossPayments()
    {
        var loan = LiveLoan();

        loan.RecordPayment(Disbursed.AddDays(30), 0m, 1_479.45m);
        loan.RecordPayment(Disbursed.AddDays(61), 20_000m, 1_000m);

        Assert.Equal(20_000m, loan.TotalPrincipalPaid);
        Assert.Equal(2_479.45m, loan.TotalInterestPaid);
        // Everything that ran, whether it was paid or carried
        Assert.Equal(1_479.45m + 1_528.77m, loan.TotalInterestAccrued);
    }

    [Fact]
    public void Payment_RecordsMoreInterestThanHasAccrued()
    {
        var loan = LiveLoan();

        // 1,479.45 has accrued by day 30; the borrower hands over a round 5,000.
        var result = loan.RecordPayment(Disbursed.AddDays(30), 0m, interestAmount: 5_000m);

        Assert.True(result.IsSuccess);
        Assert.Equal(5_000m, result.Value.InterestPaid);
        Assert.Equal(5_000m, loan.TotalInterestPaid);
        // What was owed is settled, and the surplus does not become a credit against
        // interest the loan has not earned yet.
        Assert.Equal(0m, loan.UnpaidInterest);
        Assert.Equal(1_479.45m, loan.TotalInterestAccrued);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
    }

    [Fact]
    public void PayingAheadOnInterest_DoesNotSuppressLaterAccrual()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);
        loan.RecordPayment(paidOn, 0m, interestAmount: 5_000m);

        // The next stretch runs from the payment date at the ordinary rate — the overpayment
        // bought nothing forward.
        var accrued = loan.InterestAccruedAsOf(paidOn.AddDays(30));

        Assert.Equal(1_479.45m, accrued);
    }

    [Fact]
    public void Payment_IsRejected_WhenPrincipalExceedsWhatIsOutstanding()
    {
        var loan = LiveLoan();

        var result = loan.RecordPayment(Disbursed.AddDays(30), principalAmount: 100_001m, interestAmount: 0m);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.PrincipalExceedsOutstanding", result.Error.Code);
    }

    [Fact]
    public void Payment_IsRejected_WhenDatedBeforeTheLastSettledTransaction()
    {
        var loan = LiveLoan();
        loan.RecordPayment(Disbursed.AddDays(30), 0m, 1_479.45m);

        var result = loan.RecordPayment(Disbursed.AddDays(15), 1_000m, 0m);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.PaymentBeforeLastTransaction", result.Error.Code);
    }

    [Fact]
    public void Payment_IsRejected_WhenTheLoanIsNotLive()
    {
        var loan = PendingLoan();

        var result = loan.RecordPayment(Disbursed.AddDays(30), 1_000m, 0m);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.NotActive", result.Error.Code);
    }

    [Fact]
    public void ARemainderUnderARupee_SettlesTheLoan()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        // Leaves 0.55 of principal outstanding and clears the interest.
        var result = loan.RecordPayment(paidOn, principalAmount: 99_999.45m, interestAmount: 1_479.45m);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.PaidOff, loan.Status);
        Assert.Equal(0m, loan.OutstandingPrincipal);
        Assert.Equal(0m, loan.UnpaidInterest);
    }

    [Fact]
    public void ARemainderOfARupeeOrMore_KeepsTheLoanRunning()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        var result = loan.RecordPayment(paidOn, principalAmount: 99_999m, interestAmount: 1_479.45m);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(1m, loan.OutstandingPrincipal);
    }

    [Fact]
    public void SmallResiduesAreJudgedTogetherNotSeparately()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        // 0.60 of principal and 0.55 of interest are each under a rupee, but 1.15 together
        // is a real balance, so the loan stays open.
        var result = loan.RecordPayment(paidOn, principalAmount: 99_999.40m, interestAmount: 1_478.90m);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(0.60m, loan.OutstandingPrincipal);
        Assert.Equal(0.55m, loan.UnpaidInterest);
    }

    [Fact]
    public void ACentOverWhatAccrued_ClearsTheInterestAndIsRecordedAsPaid()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);

        var result = loan.RecordPayment(paidOn, 0m, interestAmount: 1_479.46m);

        Assert.True(result.IsSuccess);
        Assert.Equal(1_479.46m, result.Value.InterestPaid);
        Assert.Equal(0m, loan.UnpaidInterest);
    }

    [Fact]
    public void SamedayPayment_AccruesNoFurtherInterest()
    {
        var loan = LiveLoan();
        var paidOn = Disbursed.AddDays(30);
        loan.RecordPayment(paidOn, 0m, 1_479.45m);

        var second = loan.RecordPayment(paidOn, principalAmount: 5_000m, interestAmount: 0m);

        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value.DaysAccrued);
        Assert.Equal(0m, second.Value.InterestOwedBefore);
        Assert.Equal(95_000m, loan.OutstandingPrincipal);
    }
}
