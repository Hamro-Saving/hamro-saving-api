using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Groups;

public static class GroupErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Group.NotFound", $"Group with ID {id} was not found.");

    public static readonly Error CodeNotUnique =
        Error.Conflict("Group.CodeNotUnique", "A group with this code already exists.");

    public static readonly Error NotActive =
        Error.Problem("Group.NotActive", "The group is not active.");

    public static readonly Error HasData =
        Error.Conflict("Group.HasData", "Cannot delete a group that has existing members or financial data.");
}
