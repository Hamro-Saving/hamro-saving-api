namespace HamroSavings.Application.NonMembers.Get;

public sealed record NonMemberResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    string? Address,
    Guid GroupId,
    bool IsActive,
    DateTime CreatedAt);
