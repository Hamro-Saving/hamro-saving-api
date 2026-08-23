using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Loans;

internal static class LoanVoting
{
    /// <summary>
    /// Votes one way needed to settle a loan: a strict majority of the group's eligible voters.
    /// </summary>
    public static int VotesNeeded(int totalVoters) => totalVoters / 2 + 1;

    /// <summary>
    /// Who gets a say on a group's loans: active full members only. Non-members may borrow
    /// but never vote, and admins disburse or cancel instead of voting — so neither is counted
    /// here. This is both the eligibility test and the denominator for the majority, so the two
    /// can never drift apart. Callers add their own group filter.
    /// </summary>
    public static IQueryable<Member> EligibleVoters(IApplicationDbContext dbContext) =>
        dbContext.Members.Where(m =>
            m.IsActive &&
            m.MembershipType == MembershipType.Member &&
            !dbContext.Users.Any(u => u.MemberId == m.Id && u.Role != UserRole.Member));
}
