namespace HamroSavings.Application.Groups.Get;

public sealed record GroupResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    decimal MemberInterestRate,
    decimal NonMemberInterestRate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MemberCount);
