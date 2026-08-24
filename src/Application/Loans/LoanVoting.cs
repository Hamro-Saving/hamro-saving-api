using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Loans;

internal static class LoanVoting
{
    /// <summary>
    /// Votes one way needed to settle a loan: a strict majority of the group's eligible voters.
    /// </summary>
    public static int VotesNeeded(int totalVoters) => totalVoters / 2 + 1;

    /// <summary>
    /// Who gets a say on a group's loans: everyone who takes part in it. An admin is a member
    /// with extra privileges, so they vote alongside the members they administer; a non-member
    /// borrows from the group without a say in it. This is both the eligibility test and the
    /// denominator for the majority, so the two can never drift apart. Callers add their own
    /// group filter.
    /// </summary>
    public static IQueryable<Member> EligibleVoters(IApplicationDbContext dbContext) =>
        dbContext.Members.Where(m =>
            m.IsActive &&
            m.GroupRole != GroupRole.NonMember);
}
