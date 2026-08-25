using HamroSavings.Application.Loans.GetLoans;
using HamroSavings.Domain.Loans;

namespace UnitTests.Loans;

/// <summary>
/// What a borrower who is not part of the group may see of their own loan: the terms,
/// never the group's deliberation over them.
/// </summary>
public class LoanResponseTests
{
    private static LoanResponse Full()
    {
        var voter = new ApproverInfo(Guid.NewGuid(), "Rajesh Sonepa", DateTime.UtcNow);

        return new LoanResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Walk In", "NonMember", Guid.NewGuid(),
            50_000m, 18m, 50_000m, 100m, 50_100m, 25m, 100m, 0m, 0m,
            DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, null,
            LoanStatus.Active, "notes", Guid.NewGuid(),
            IsForceDisbursed: true,
            ApprovalCount: 3, DeclineCount: 1, RequiredApprovals: 4, RequiredDeclines: 3,
            HasCurrentUserApproved: true, HasCurrentUserDeclined: false,
            Approvers: [voter], Decliners: [voter],
            CreatedAt: DateTime.UtcNow);
    }

    [Fact]
    public void StrippingGovernanceLeavesTheLoanTermsIntact()
    {
        var full = Full();
        var stripped = full.WithoutGroupInternals();

        Assert.Equal(full.Amount, stripped.Amount);
        Assert.Equal(full.InterestRate, stripped.InterestRate);
        Assert.Equal(full.OutstandingPrincipal, stripped.OutstandingPrincipal);
        Assert.Equal(full.PayoffAmount, stripped.PayoffAmount);
        Assert.Equal(full.Status, stripped.Status);
        Assert.Equal(full.DueDate, stripped.DueDate);
    }

    [Fact]
    public void StrippingGovernanceRemovesEveryTraceOfTheVote()
    {
        var stripped = Full().WithoutGroupInternals();

        // Approver names are the leak that matters: the roster is private to a non-member,
        // so it must not arrive by way of their own loan.
        Assert.Empty(stripped.Approvers);
        Assert.Empty(stripped.Decliners);
        Assert.Equal(0, stripped.ApprovalCount);
        Assert.Equal(0, stripped.DeclineCount);
        Assert.Equal(0, stripped.RequiredApprovals);
        Assert.Equal(0, stripped.RequiredDeclines);
        // How the loan passed is the group's business too — a forced payout says the
        // members were bypassed, which is a fact about the group, not about the borrower.
        Assert.False(stripped.IsForceDisbursed);
        Assert.False(stripped.HasCurrentUserApproved);
        Assert.False(stripped.HasCurrentUserDeclined);
        Assert.Null(stripped.DisbursedById);
    }
}
