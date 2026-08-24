using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Members.Get;

public sealed record MemberResponse(
    Guid Id,
    string? Email,
    string FirstName,
    string? LastName,
    string FullName,
    GroupRole GroupRole,
    Guid GroupId,
    bool IsActive,
    bool HasAccount,
    string? PhoneNumber,
    string? Address,
    DateTime CreatedAt);
