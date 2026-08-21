using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Groups.SetValidity;

public sealed record SetGroupValidityCommand(
    Guid GroupId,
    bool IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo) : ICommand;
