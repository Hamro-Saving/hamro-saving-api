using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Groups.Create;

public sealed record CreateGroupCommand(
    string Name,
    string Code,
    string? Description,
    decimal MemberInterestRate,
    decimal NonMemberInterestRate) : ICommand<Guid>;
