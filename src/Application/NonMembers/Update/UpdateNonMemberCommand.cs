using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.NonMembers.Update;

public sealed record UpdateNonMemberCommand(
    Guid NonMemberId,
    string FullName,
    string? Email,
    string? Phone,
    string? Address) : ICommand;
