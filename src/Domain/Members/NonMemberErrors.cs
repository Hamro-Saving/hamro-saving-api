using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

public static class NonMemberErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("NonMember.NotFound", $"Non-member with ID {id} was not found.");

    public static readonly Error NotInGroup =
        Error.Forbidden("NonMember.NotInGroup", "Non-member does not belong to this group.");
}
