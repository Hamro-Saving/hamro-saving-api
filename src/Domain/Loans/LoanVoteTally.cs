namespace HamroSavings.Domain.Loans;

/// <summary>
/// Where a loan's vote stands. Only the declines matter here: approvals settle a loan on
/// their own through the normal route, while a force disbursement is precisely the case
/// where they never arrived.
/// </summary>
public readonly record struct LoanVoteTally(int Declines, int DeclinesNeeded)
{
    /// <summary>
    /// Whether the group has said no. A tally is re-read at the moment of forcing rather than
    /// trusted from when the votes were cast: the threshold moves with the size of the group,
    /// so a loan that fell short of a refusal can cross it later when a member leaves.
    /// </summary>
    public bool GroupHasRefused => DeclinesNeeded > 0 && Declines >= DeclinesNeeded;
}
