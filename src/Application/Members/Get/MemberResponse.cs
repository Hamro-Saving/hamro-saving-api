using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Members.Get;

public sealed record MemberResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    UserRole Role,
    Guid? GroupId,
    bool IsActive,
    DateTime CreatedAt);
