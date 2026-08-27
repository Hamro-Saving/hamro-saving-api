using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Loans;

internal static class LoanVoting
{
    /// <summary>
    /// Approvals needed to pass a loan: a strict majority of the voters the loan actually has,
    /// so lending out the group's money always takes more than half of them. The count passed
    /// in must come from <see cref="VotersOn"/> — measuring against the whole group instead
    /// would demand approvals from someone who is barred from giving one.
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

    /// <summary>
    /// The voters a particular loan actually has: everyone eligible in the group except the
    /// borrower, who is refused a vote on their own request. This is the denominator every
    /// threshold must be measured against — counting the whole group instead leaves the
    /// borrower's own vote in the total while the vote itself can never be cast, so a group
    /// of four needed all three of the others to agree.
    ///
    /// A non-member borrower is not in the eligible set to begin with, so excluding by id is
    /// simply a no-op there and one expression covers both kinds of loan.
    /// </summary>
    public static IQueryable<Member> VotersOn(IApplicationDbContext dbContext, Guid groupId, Guid borrowerId) =>
        EligibleVoters(dbContext).Where(m => m.GroupId == groupId && m.Id != borrowerId);
}
