namespace HamroSavings.Domain.Members;

/// <summary>
/// What a person is inside one group — the single group-level axis of authorization.
/// The three are mutually exclusive: a NonMember borrows but never deposits or votes,
/// a Member is a full participant, and an Admin runs the group.
/// </summary>
public enum GroupRole
{
    /// <summary>Borrows from the group without participating in it. No deposits, no vote.</summary>
    NonMember = 0,

    /// <summary>Full participant: deposits, borrows, and votes on loans.</summary>
    Member = 1,

    /// <summary>Runs the group. Verifies deposits, disburses loans, manages the roster.</summary>
    Admin = 2,
}

public static class GroupRoleExtensions
{
    /// <summary>Members and admins alike take part in the group; non-members only borrow from it.</summary>
    public static bool Participates(this GroupRole role) => role != GroupRole.NonMember;
}
