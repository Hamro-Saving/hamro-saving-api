using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;

namespace UnitTests.Notifications;

/// <summary>
/// Each moment is announced exactly once and only when it happened. A member emailed twice
/// about one loan, or told the group approved what an admin forced through, stops trusting
/// the emails.
/// </summary>
public class LoanNotificationEventTests
{
    private static readonly CashInHand Funded = new(10_000_000m);
    private static readonly DateTime Start = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Loan NewLoan() =>
        Loan.Create(Guid.NewGuid(), "Member", Guid.NewGuid(), 100_000m, 18m, Start, null, null);

    private static T Single<T>(Entity entity) where T : IDomainEvent =>
        Assert.Single(entity.DomainEvents.OfType<T>());

    [Fact]
    public void ARequestedLoanAsksTheGroupToVote()
    {
        var loan = NewLoan();

        var raised = Single<LoanRequestedDomainEvent>(loan);
        Assert.Equal(loan.Id, raised.LoanId);
        Assert.Equal(loan.GroupId, raised.GroupId);
        Assert.Equal(loan.BorrowerId, raised.BorrowerId);
    }

    [Fact]
    public void ASettledVoteSaysWhichWayItWent()
    {
        var approved = NewLoan();
        approved.ClearDomainEvents();
        approved.ApproveLoan();
        Assert.True(Single<LoanVoteSettledDomainEvent>(approved).IsApproved);

        var declined = NewLoan();
        declined.ClearDomainEvents();
        declined.Decline();
        Assert.False(Single<LoanVoteSettledDomainEvent>(declined).IsApproved);
    }

    [Fact]
    public void ARefusedTransitionAnnouncesNothing()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.ClearDomainEvents();

        // Already approved, so this fails — and a failed transition must stay silent.
        Assert.True(loan.ApproveLoan().IsFailure);
        Assert.Empty(loan.DomainEvents);
    }

    [Fact]
    public void DisbursementIsItsOwnAnnouncement()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.ClearDomainEvents();

        Assert.True(loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded).IsSuccess);

        var raised = Single<LoanDisbursedDomainEvent>(loan);
        Assert.Equal(loan.Id, raised.LoanId);
        // The loan is not re-announced as a fresh request when the money goes out.
        Assert.Empty(loan.DomainEvents.OfType<LoanRequestedDomainEvent>());
    }

    [Fact]
    public void ForcingALoanThroughAnnouncesThePayoutButNotAVote()
    {
        var loan = NewLoan();
        loan.ClearDomainEvents();

        var result = loan.ForceDisbursement(Guid.NewGuid(), Start, Funded, new LoanVoteTally(0, 2));

        Assert.True(result.IsSuccess);
        Assert.Single(loan.DomainEvents.OfType<LoanDisbursedDomainEvent>());
        // An admin's say-so is not the group's decision and must never be reported as one.
        Assert.Empty(loan.DomainEvents.OfType<LoanVoteSettledDomainEvent>());
    }

    [Fact]
    public void ARevisionPutsTheLoanBackToTheGroup()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.ClearDomainEvents();

        // Revising clears the votes, so the members have to be asked all over again.
        Assert.True(loan.Revise(60_000m, 18m, Start, null, null).IsSuccess);
        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal(loan.Id, Single<LoanRequestedDomainEvent>(loan).LoanId);
    }

    [Fact]
    public void APaymentAnnouncesItselfWhenRecordedAndAgainWhenVerified()
    {
        var loan = NewLoan();
        loan.ApproveLoan();
        loan.CompleteDisbursement(Guid.NewGuid(), Start, Funded);

        var allocation = loan.RecordPayment(Start.AddDays(30), 10_000m, 1_479m);
        Assert.True(allocation.IsSuccess);

        var payment = LoanPayment.Create(loan.Id, Start.AddDays(30), allocation.Value, null, Guid.NewGuid());
        Assert.Equal(payment.Id, Single<LoanPaymentRecordedDomainEvent>(payment).PaymentId);

        payment.ClearDomainEvents();
        Assert.True(payment.Verify(Guid.NewGuid()).IsSuccess);
        Assert.Equal(loan.Id, Single<LoanPaymentVerifiedDomainEvent>(payment).LoanId);

        // Verifying twice fails, and a second email must not go out.
        payment.ClearDomainEvents();
        Assert.True(payment.Verify(Guid.NewGuid()).IsFailure);
        Assert.Empty(payment.DomainEvents);
    }
}
