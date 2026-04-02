using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.Delete;

public sealed record DeleteMemberCommand(Guid MemberId) : ICommand;
