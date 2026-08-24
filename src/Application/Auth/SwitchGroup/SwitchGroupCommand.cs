using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.SwitchGroup;

public sealed record SwitchGroupCommand(Guid GroupId) : ICommand<string>;
