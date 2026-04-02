using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Groups.Update;

public sealed record UpdateGroupCommand(
    Guid GroupId,
    string Name,
    string? Description,
    decimal MemberInterestRate,
    decimal NonMemberInterestRate) : ICommand;
