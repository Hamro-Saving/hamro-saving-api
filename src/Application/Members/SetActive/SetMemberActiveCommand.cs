using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.SetActive;

public sealed record SetMemberActiveCommand(Guid MemberId, bool IsActive) : ICommand;
