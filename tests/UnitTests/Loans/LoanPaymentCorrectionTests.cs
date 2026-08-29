using HamroSavings.Application.Loans;
using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;

namespace UnitTests.Loans;

/// <summary>
/// Correcting or removing a payment that has not been verified. Each payment settles the
/// interest that had run up to its date, so a change to one has to carry through everything
/// after it: the loan is wound back to disbursement and its remaining payments applied again.
/// </summary>
public class LoanPaymentCorrectionTests
{
    private static readonly CashInHand Funded = new(10_000_000m);

    private static readonly DateTime Disbursed = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>A live loan of 100,000 at 18% a year — 49.315068 a day — disbursed 2026-06-01.</summary>
    private static Loan LiveLoan(decimal amount = 100_000m)
    {
        var loan = Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), amount, 18m, Disbursed, null, null);
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Disbursed, Funded);
        return loan;
    }

    private static LoanPayment Pay(Loan loan, DateTime on, decimal principal, decimal interest)
    {
        var allocation = loan.RecordPayment(on, principal, interest);
        Assert.True(allocation.IsSuccess);
        return LoanPayment.Create(loan.Id, on, allocation.Value, null, Guid.NewGuid());
    }

    [Fact]
    public void RemovingTheOnlyPayment_PutsTheLoanBackWhereItWas()
    {
        var loan = LiveLoan();
        Pay(loan, Disbursed.AddDays(30), principal: 20_000m, interest: 1_479.45m);

        var result = LoanPaymentReplay.Apply(loan, []);

        Assert.True(result.IsSuccess);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
        Assert.Equal(0m, loan.TotalPrincipalPaid);
        Assert.Equal(0m, loan.TotalInterestPaid);
        Assert.Equal(Disbursed, loan.LastAccrualDate);
        // The interest that payment cleared runs again
        Assert.Equal(1_479.45m, loan.InterestAccruedAsOf(Disbursed.AddDays(30)));
    }

    [Fact]
    public void RemovingThePaymentThatClearedTheLoan_MakesItLiveAgain()
    {
        var loan = LiveLoan();
        Pay(loan, Disbursed.AddDays(30), principal: 100_000m, interest: 1_479.45m);
        Assert.Equal(LoanStatus.PaidOff, loan.Status);

        LoanPaymentReplay.Apply(loan, []);

        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(100_000m, loan.OutstandingPrincipal);
    }

    [Fact]
    public void CorrectingAPayment_CarriesThroughToTheOnesAfterIt()
    {
        var loan = LiveLoan();
        var first = Pay(loan, Disbursed.AddDays(30), principal: 20_000m, interest: 1_479.45m);
        var second = Pay(loan, Disbursed.AddDays(60), principal: 10_000m, interest: 1_183.56m);

        // 20,000 of principal was entered when 30,000 was handed over
        first.Revise(first.PaidDate, principalAmount: 30_000m, interestAmount: 1_479.45m, notes: null);
        var result = LoanPaymentReplay.Apply(loan, [first, second]);

        Assert.True(result.IsSuccess);
        Assert.Equal(60_000m, loan.OutstandingPrincipal);
        Assert.Equal(40_000m, loan.TotalPrincipalPaid);
        Assert.Equal(70_000m, first.OutstandingPrincipalAfter);
        Assert.Equal(60_000m, second.OutstandingPrincipalAfter);
        // The second stretch now runs on 70,000, not 80,000: 30 × 34.520548 = 1,035.62
        Assert.Equal(1_035.62m, second.InterestOwedBefore);
        // 1,183.56 was paid against 1,035.62 owed — the surplus is not carried as a credit
        Assert.Equal(0m, second.UnpaidInterestAfter);
    }

    [Fact]
    public void CorrectingAPayment_CannotPutMoreTowardsPrincipalThanIsOwed()
    {
        var loan = LiveLoan();
        var payment = Pay(loan, Disbursed.AddDays(30), principal: 20_000m, interest: 1_479.45m);

        payment.Revise(payment.PaidDate, principalAmount: 120_000m, interestAmount: 1_479.45m, notes: null);
        var result = LoanPaymentReplay.Apply(loan, [payment]);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.PrincipalExceedsOutstanding, result.Error);
    }

    [Fact]
    public void AVerifiedPayment_CannotBeRevised()
    {
        var loan = LiveLoan();
        var payment = Pay(loan, Disbursed.AddDays(30), principal: 20_000m, interest: 1_479.45m);
        payment.Verify(Guid.NewGuid());

        var result = payment.Revise(payment.PaidDate, 25_000m, 1_479.45m, null);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.CannotModifyVerifiedPayment, result.Error);
    }

    [Fact]
    public void AVerifiedPaymentAfterIt_BlocksTheCorrection()
    {
        var loan = LiveLoan();
        var first = Pay(loan, Disbursed.AddDays(30), principal: 20_000m, interest: 1_479.45m);
        var second = Pay(loan, Disbursed.AddDays(60), principal: 10_000m, interest: 1_183.56m);
        second.Verify(Guid.NewGuid());

        // Replaying would restate the verified payment while its ledger entries stayed put
        Assert.True(LoanPaymentReplay.HasVerifiedPaymentAfter(first, [first, second]));
        // The other way round nothing posted moves, so it is allowed
        Assert.False(LoanPaymentReplay.HasVerifiedPaymentAfter(second, [first, second]));
    }

    [Fact]
    public void AnUndisbursedLoan_HasNothingToRewindTo()
    {
        var loan = Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), 100_000m, 18m, Disbursed, null, null);

        var result = loan.RewindToDisbursement();

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.NotDisbursed, result.Error);
    }
}
