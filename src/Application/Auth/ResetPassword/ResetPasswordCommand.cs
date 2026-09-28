using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    Guid Token,
    string Password) : ICommand;
