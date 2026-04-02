using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Members.Create;

public sealed record CreateMemberCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid GroupId) : ICommand<Guid>;
