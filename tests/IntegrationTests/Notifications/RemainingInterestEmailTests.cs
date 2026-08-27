using HamroSavings.Infrastructure.Email;
using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;

namespace IntegrationTests.Notifications;

/// <summary>
/// Clearing the principal is the moment a borrower believes they are finished. If interest is
/// still owed the loan is not closed, so the email has to say so rather than leaving them to
/// read it off a table.
/// </summary>
public class RemainingInterestEmailTests
{
    private static readonly CashInHand Funded = new(10_000_000m);
    private static readonly DateTime Start = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Loan ActiveLoan()
    {
        var loan = Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), 100_000m, 18m, Start, null, null);
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);
        return loan;
    }

    private static LoanPayment PaymentFrom(Loan loan, DateTime on, decimal principal, decimal interest)
    {
        var allocation = loan.RecordPayment(on, principal, interest);
        Assert.True(allocation.IsSuccess);
        return LoanPayment.Create(loan.Id, on, allocation.Value, null, Guid.NewGuid());
    }

    [Fact]
    public void ClearingThePrincipalWhileInterestRemainsDoesNotCloseTheLoan()
    {
        var loan = ActiveLoan();
        var payDay = Start.AddDays(90);

        // The whole principal, and none of the interest that ran on it.
        var payment = PaymentFrom(loan, payDay, principal: 100_000m, interest: 0m);

        Assert.Equal(0m, loan.OutstandingPrincipal);
        Assert.True(loan.UnpaidInterest > 0);
        // Still owing means still open — this must not read as PaidOff anywhere.
        Assert.Equal(LoanStatus.Active, loan.Status);

        var warning = LoanPaymentNarrative.InterestStillOwed(payment);

        Assert.NotNull(warning);
        Assert.Contains("The principal is now fully repaid", warning);
        Assert.Contains(Money.Format(loan.UnpaidInterest), warning);
        Assert.Contains("no further interest will accrue", warning);
    }

    [Fact]
    public void NothingFurtherAccruesOnceThePrincipalIsGone()
    {
        var loan = ActiveLoan();
        loan.RecordPayment(Start.AddDays(90), 100_000m, 0m);

        var owed = loan.UnpaidInterest;

        // Interest runs on outstanding principal, and there is none — so the figure the email
        // quotes is the whole of what is left, not a number that grows while they arrange it.
        Assert.Equal(0m, loan.DailyInterest);
        Assert.Equal(owed, loan.InterestAccruedAsOf(Start.AddDays(365)));
    }

    [Fact]
    public void APaymentThatLeavesPrincipalOutstandingSaysNothingAboutIt()
    {
        var loan = ActiveLoan();

        var payment = PaymentFrom(loan, Start.AddDays(30), principal: 10_000m, interest: 0m);

        Assert.True(loan.OutstandingPrincipal > 0);
        // The borrower is plainly not finished, so there is nothing to warn them about.
        Assert.Null(LoanPaymentNarrative.InterestStillOwed(payment));
    }

    [Fact]
    public void ASettledLoanSaysNothingAboutIt()
    {
        var loan = ActiveLoan();
        var payDay = Start.AddDays(90);
        var payoff = loan.PayoffAmountAsOf(payDay);

        var payment = PaymentFrom(loan, payDay, principal: 100_000m, interest: payoff - 100_000m);

        Assert.Equal(LoanStatus.PaidOff, loan.Status);
        Assert.Null(LoanPaymentNarrative.InterestStillOwed(payment));
    }

    [Fact]
    public void RoundingRemaindersDoNotProduceAWarningAboutNothing()
    {
        var loan = ActiveLoan();
        var payDay = Start.AddDays(90);
        var owed = loan.PayoffAmountAsOf(payDay);

        // A few paisa short. The loan writes those off and settles, so there is no interest
        // left to chase and no warning to send.
        var payment = PaymentFrom(loan, payDay, principal: 100_000m, interest: owed - 100_000m - 0.40m);

        Assert.Equal(LoanStatus.PaidOff, loan.Status);
        Assert.Null(LoanPaymentNarrative.InterestStillOwed(payment));
    }
}
