using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.NonMembers.Create;

public sealed record CreateNonMemberCommand(
    string FullName,
    string? Email,
    string? Phone,
    string? Address,
    Guid GroupId) : ICommand<Guid>;
