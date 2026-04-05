using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : ICommand<Guid>;
