using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Members.Get;

public sealed record MemberResponse(
    Guid Id,
    string? Email,
    string FirstName,
    string? LastName,
    string FullName,
    UserRole? Role,
    MembershipType MembershipType,
    Guid GroupId,
    bool IsActive,
    bool HasAccount,
    string? PhoneNumber,
    string? Address,
    DateTime CreatedAt);
