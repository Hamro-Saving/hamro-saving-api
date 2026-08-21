using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.RemoveAdmin;

public sealed record RemoveAdminCommand(Guid MemberId) : ICommand;
