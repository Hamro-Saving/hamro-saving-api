using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
