using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Members.Update;

public sealed record UpdateMemberCommand(
    Guid MemberId,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role) : ICommand;
