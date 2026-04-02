using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<string>;
