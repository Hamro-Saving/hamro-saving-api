using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.Signup;

public sealed record SignupWithTokenCommand(
    Guid Token,
    string Password) : ICommand<string>;
