using HamroSavings.Application.Loans;
using HamroSavings.Domain.Members;

namespace UnitTests.Loans;

/// <summary>
/// Who votes on a loan. An admin is a member with extra privileges, so they vote alongside
/// the members they administer; a non-member borrows from the group without a say in it.
/// </summary>
public class LoanVotingTests
{
    [Theory]
    [InlineData(GroupRole.Member, true)]
    [InlineData(GroupRole.Admin, true)]
    [InlineData(GroupRole.NonMember, false)]
    public void OnlyParticipantsVote(GroupRole role, bool votes)
    {
        Assert.Equal(votes, role.Participates());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(5, 3)]
    [InlineData(6, 4)]
    public void PassingALoanTakesMoreThanHalfTheVoters(int voters, int needed)
    {
        Assert.Equal(needed, LoanVoting.ApprovalsNeeded(voters));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    public void RefusingALoanTakesHalfTheVoters(int voters, int needed)
    {
        Assert.Equal(needed, LoanVoting.DeclinesNeeded(voters));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(10)]
    public void AnEvenlySplitGroupDeclines(int voters)
    {
        // Half of an even group is exactly 50%, which refuses but does not pass:
        // a group that cannot agree should not be lending its money out.
        var half = voters / 2;

        Assert.True(half >= LoanVoting.DeclinesNeeded(voters));
        Assert.False(half >= LoanVoting.ApprovalsNeeded(voters));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(13)]
    public void RefusingIsNeverHarderThanPassing(int voters)
    {
        Assert.True(LoanVoting.DeclinesNeeded(voters) <= LoanVoting.ApprovalsNeeded(voters));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(6, 3)]
    [InlineData(7, 4)]
    public void TheDeclineThresholdIsAtLeastHalfTheVoters(int voters, int needed)
    {
        // The rule is "50% or more", so the threshold over the total must reach a half.
        Assert.True((double)needed / voters >= 0.5);
        // ...and one fewer vote must not, or the bar would be lower than intended.
        Assert.True((double)(needed - 1) / voters < 0.5);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    [InlineData(7, 4)]
    public void AMemberBorrowingIsLeftOutOfTheirOwnVote(int groupMembers, int needed)
    {
        // The borrower is refused a vote on their own request, so they are not part of the
        // total that decides it either. The threshold is a majority of everyone else.
        Assert.Equal(needed, LoanVoting.ApprovalsNeeded(groupMembers - 1));
    }

    [Fact]
    public void CountingTheBorrowerUsedToDemandUnanimity()
    {
        // A group of four with one member borrowing has three people who can vote. Measuring
        // against all four asked for three approvals — every last one of them — because the
        // borrower's own uncastable vote was still sitting in the total.
        Assert.Equal(3, LoanVoting.ApprovalsNeeded(4));
        Assert.Equal(2, LoanVoting.ApprovalsNeeded(3));
    }

    [Fact]
    public void ANonMemberBorrowingCostsTheGroupNoVoters()
    {
        // A non-member is not an eligible voter, so a loan to one has nobody to leave out:
        // all four members still decide it, and the bar stays where it was.
        Assert.Equal(3, LoanVoting.ApprovalsNeeded(4));
    }

    [Fact]
    public void CountingAdminsAsVotersRaisesTheBarForApproval()
    {
        // A group of 2 members and 2 admins used to settle a loan on 2 votes,
        // because only the members counted. Now all four vote and it takes 3.
        Assert.Equal(2, LoanVoting.ApprovalsNeeded(2));
        Assert.Equal(3, LoanVoting.ApprovalsNeeded(4));
    }
}
