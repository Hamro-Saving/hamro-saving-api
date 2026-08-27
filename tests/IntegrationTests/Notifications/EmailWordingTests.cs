using HamroSavings.Infrastructure.Email;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;

namespace IntegrationTests.Notifications;

/// <summary>
/// The wording is the whole feature: a member skims one line on their phone and has to come
/// away knowing who paid what, for which month, and whether anything is being asked of them.
/// </summary>
public class EmailWordingTests
{
    private static Deposit Deposit_(DepositType type, int? month, int? year, decimal amount) =>
        Deposit.Create(Guid.NewGuid(), Guid.NewGuid(), amount, month, year,
            DateOnly.FromDateTime(DateTime.UtcNow), type, null, Guid.NewGuid());

    [Fact]
    public void AMonthlyDepositIsNamedByItsBikramSambatPeriod()
    {
        var deposit = Deposit_(DepositType.MonthlyDeposit, month: 5, year: 2082, amount: 5_000m);

        Assert.Equal("monthly deposit of Rs. 5,000.00 for Bhadra 2082", DepositNarrative.Describe(deposit));
    }

    [Fact]
    public void ADepositWithNoPeriodDoesNotInventOne()
    {
        // Only a monthly deposit carries a month, so anything else must not claim a period.
        var deposit = Deposit_(DepositType.Other, month: 5, year: 2082, amount: 1_200m);

        Assert.Equal("deposit of Rs. 1,200.00", DepositNarrative.Describe(deposit));
        Assert.DoesNotContain(DepositNarrative.Figures(deposit, "Sita"), d => d.Label == "For the month of");
    }

    [Fact]
    public void TheFiguresLeadWithWhoAndHowMuch()
    {
        var deposit = Deposit_(DepositType.MonthlyDeposit, 5, 2082, 5_000m);

        var figures = DepositNarrative.Figures(deposit, "Sita Rai");

        Assert.Equal("Sita Rai", figures[0].Value);
        Assert.Equal("Rs. 5,000.00", figures[1].Value);
        Assert.Contains(figures, d => d is { Label: "For the month of", Value: "Bhadra 2082" });
    }

    [Fact]
    public void ANonMemberBorrowerIsMarkedAsOne()
    {
        var member = Member.Create("Ram", "Bahadur", "ram@example.com", null, Guid.NewGuid());
        var outsider = Member.CreateNonMember("Hari Thapa", null, null, null, Guid.NewGuid());

        Assert.Equal("Ram Bahadur", LoanNarrative.Borrower(member));
        // The members are lending the group's money to someone outside it. That is the single
        // most important fact in the email and it is never left to be inferred.
        Assert.Equal("Hari Thapa (non-member)", LoanNarrative.Borrower(outsider));
    }

    [Fact]
    public void APaymentOfOneKindDoesNotReciteAZeroForTheOther()
    {
        Assert.Equal("interest payment of Rs. 1,500.00",
            LoanPaymentNarrative.Describe(PaymentOf(principal: 0m, interest: 1_500m)));

        Assert.Equal("principal repayment of Rs. 10,000.00",
            LoanPaymentNarrative.Describe(PaymentOf(principal: 10_000m, interest: 0m)));

        Assert.Equal("loan payment of Rs. 11,500.00",
            LoanPaymentNarrative.Describe(PaymentOf(principal: 10_000m, interest: 1_500m)));
    }

    [Fact]
    public void MoneyIsWrittenTheWayTheBooksWriteIt()
    {
        Assert.Equal("Rs. 0.00", Money.Format(0m));
        Assert.Equal("Rs. 1,234,567.89", Money.Format(1234567.89m));
    }

    private static LoanPayment PaymentOf(decimal principal, decimal interest) =>
        LoanPayment.Create(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new LoanPaymentAllocation(interest, 30, interest, principal, 0m, 0m),
            null,
            Guid.NewGuid());
}
