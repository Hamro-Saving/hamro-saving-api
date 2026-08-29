using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

public static class MemberErrors
{
    public static Error NotFound(Guid memberId) =>
        Error.NotFound("Member.NotFound", $"Member with ID '{memberId}' was not found.");

    public static Error NoLinkedUser(Guid memberId) =>
        Error.Problem("Member.NoLinkedUser", $"Member '{memberId}' has no login and cannot be made an admin.");

    public static Error NoEmailToInvite(Guid memberId) =>
        Error.Validation("Member.NoEmailToInvite", $"Member '{memberId}' has no email address to invite.");

    public static readonly Error InviteEmailFailed =
        Error.Problem("Member.InviteEmailFailed", "The invite link was regenerated, but the email could not be sent. Check the mail settings and try again.");

    public static readonly Error InactiveBorrower =
        Error.Problem("Member.InactiveBorrower", "This person has been deactivated and cannot be given a new loan.");

    public static readonly Error LastAdmin =
        Error.Conflict("Member.LastAdmin", "This is the group's only admin. Assign another admin first.");

    public static readonly Error EmailNotUnique =
        Error.Conflict("Member.EmailNotUnique", "A member with this email already exists in the group.");
}
