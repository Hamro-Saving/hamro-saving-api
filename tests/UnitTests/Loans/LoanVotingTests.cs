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
    public void AMajorityIsMoreThanHalfTheVoters(int voters, int needed)
    {
        Assert.Equal(needed, LoanVoting.VotesNeeded(voters));
    }

    [Fact]
    public void CountingAdminsAsVotersRaisesTheBarForApproval()
    {
        // A group of 2 members and 2 admins used to settle a loan on 2 votes,
        // because only the members counted. Now all four vote and it takes 3.
        const int membersOnly = 2;
        const int membersAndAdmins = 4;

        Assert.Equal(2, LoanVoting.VotesNeeded(membersOnly));
        Assert.Equal(3, LoanVoting.VotesNeeded(membersAndAdmins));
    }
}
