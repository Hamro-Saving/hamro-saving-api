using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Groups.Delete;

public sealed record DeleteGroupCommand(Guid GroupId) : ICommand;
