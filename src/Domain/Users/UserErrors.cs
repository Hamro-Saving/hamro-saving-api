using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("User.NotFound", $"User with ID {id} was not found.");

    public static readonly Error EmailNotUnique =
        Error.Conflict("User.EmailNotUnique", "A user with this email already exists.");

    public static readonly Error InvalidCredentials =
        Error.Problem("User.InvalidCredentials", "Invalid email or password.");

    public static readonly Error NotActive =
        Error.Problem("User.NotActive", "The user account is not active.");

    public static readonly Error Unauthorized =
        Error.Forbidden("User.Unauthorized", "You are not authorized to perform this action.");

    public static readonly Error NotInGroup =
        Error.Forbidden("User.NotInGroup", "You do not have access to this group.");
}
