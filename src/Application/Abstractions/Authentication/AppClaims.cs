namespace HamroSavings.Application.Abstractions.Authentication;

/// <summary>Claim names shared by the token writer, the reader, and the endpoint policies.</summary>
public static class AppClaims
{
    public const string IsSuperAdmin = "is_super_admin";
    public const string GroupId = "GroupId";
    public const string MemberId = "MemberId";
    public const string GroupRole = "group_role";
    public const string Memberships = "memberships";
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
}

/// <summary>Authorization policy names, applied at the endpoint level.</summary>
public static class Policies
{
    /// <summary>Platform administrator. Says nothing about group membership.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Admin of the group currently being acted in.</summary>
    public const string GroupAdmin = "GroupAdmin";

    /// <summary>Takes part in the group currently being acted in — a member or its admin.</summary>
    public const string GroupMember = "GroupMember";

    /// <summary>May read a group's books: a participant, or a SuperAdmin reading across groups.</summary>
    public const string GroupRead = "GroupRead";
}
