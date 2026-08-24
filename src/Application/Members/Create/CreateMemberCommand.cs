using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Create;

public sealed record CreateMemberCommand(
    GroupRole GroupRole,
    string FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    Guid? GroupId = null) : ICommand<Guid>;
