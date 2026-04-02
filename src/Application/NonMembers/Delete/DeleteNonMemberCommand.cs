using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.NonMembers.Delete;

public sealed record DeleteNonMemberCommand(Guid NonMemberId) : ICommand;
