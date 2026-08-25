using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Loans;

internal static class LoanVoting
{
    /// <summary>
    /// Approvals needed to pass a loan: a strict majority of the group's eligible voters,
    /// so lending out the group's money always takes more than half of them.
    /// </summary>
    public static int ApprovalsNeeded(int totalVoters) => totalVoters / 2 + 1;

    /// <summary>
    /// Declines needed to refuse a loan: half the voters, not more than half. Refusing is
    /// deliberately the easier of the two — a group that is evenly split should not be
    /// lending — so with an even number of voters an exact tie settles as declined.
    /// </summary>
    public static int DeclinesNeeded(int totalVoters) => (totalVoters + 1) / 2;

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
