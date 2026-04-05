using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

public static class MemberErrors
{
    public static Error NotFound(Guid memberId) =>
        Error.NotFound("Member.NotFound", $"Member with ID '{memberId}' was not found.");

    public static readonly Error EmailNotUnique =
        Error.Conflict("Member.EmailNotUnique", "A member with this email already exists in the group.");
}
