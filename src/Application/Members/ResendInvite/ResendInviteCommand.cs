using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.ResendInvite;

public sealed record ResendInviteCommand(Guid MemberId) : ICommand;
