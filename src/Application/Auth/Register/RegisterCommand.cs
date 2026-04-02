using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? GroupId) : ICommand<Guid>;
