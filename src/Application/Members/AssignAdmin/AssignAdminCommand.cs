using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Members.AssignAdmin;

public sealed record AssignAdminCommand(Guid MemberId) : ICommand;
