using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.Update;

public sealed record UpdateMemberCommand(
    Guid MemberId,
    string FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Address) : ICommand;
