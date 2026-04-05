using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Create;

public sealed record CreateMemberCommand(
    MembershipType MembershipType,
    string FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    Guid GroupId) : ICommand<Guid>;
